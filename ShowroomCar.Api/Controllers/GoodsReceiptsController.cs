using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShowroomCar.Application.Dtos;
using ShowroomCar.Infrastructure.Persistence.Entities;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace ShowroomCar.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "RequireAdmin")]
    public class GoodsReceiptsController : ControllerBase
    {
        private readonly ShowroomDbContext _db;
        private readonly IHttpContextAccessor _http;
        public GoodsReceiptsController(ShowroomDbContext db, IHttpContextAccessor http)
        {
            _db = db; _http = http;
        }

        private static string NewNo(string pfx) => $"{pfx}-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        private long? CurrentUserId()
        {
            var u = _http.HttpContext?.User;
            var sub = u?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? u?.FindFirst("sub")?.Value;
            return long.TryParse(sub, out var id) ? id : (long?)null;
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<GoodsReceiptDto>> Get(long id)
        {
            var gr = await _db.GoodsReceipts
                .AsNoTracking()
                .Include(g => g.GoodsReceiptItems)
                .FirstOrDefaultAsync(g => g.GrId == id);
            if (gr == null) return NotFound();

            var vins = await _db.Vehicles
                .Where(v => gr.GoodsReceiptItems.Select(i => i.VehicleId).Contains(v.VehicleId))
                .Select(v => new { v.VehicleId, v.Vin, v.EngineNo }).ToListAsync();

            var dto = new GoodsReceiptDto
            {
                GrId = gr.GrId,
                GrNo = gr.GrNo,
                PoId = gr.PoId,
                ReceiptDate = gr.ReceiptDate,
                WarehouseId = gr.WarehouseId,
                Items = gr.GoodsReceiptItems.Select(i => new GoodsReceiptItemDto
                {
                    GrItemId = i.GrItemId,
                    GrId = i.GrId,
                    VehicleId = i.VehicleId,
                    Vin = vins.FirstOrDefault(x => x.VehicleId == i.VehicleId)?.Vin ?? "",
                    EngineNo = vins.FirstOrDefault(x => x.VehicleId == i.VehicleId)?.EngineNo ?? "",
                    LandedCost = i.LandedCost ?? 0
                }).ToList()
            };
            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<object>> Create(GoodsReceiptCreateRequest req)
        {
            if (req.Vehicles == null || req.Vehicles.Count == 0)
                return BadRequest("Vehicles is empty.");

            var wh = await _db.Warehouses.FindAsync(req.WarehouseId);
            if (wh == null) return NotFound("Warehouse not found.");

            if (req.PoId.HasValue)
            {
                var po = await _db.PurchaseOrders.AsNoTracking().FirstOrDefaultAsync(p => p.PoId == req.PoId.Value);
                if (po == null) return BadRequest("PO not found.");
            }

            // VIN/Engine uniqueness pre-check
            var vins = req.Vehicles.Select(v => v.Vin).ToList();
            var engs = req.Vehicles.Select(v => v.EngineNo).ToList();
            var vinExists = await _db.Vehicles.AnyAsync(v => vins.Contains(v.Vin));
            var engExists = await _db.Vehicles.AnyAsync(v => engs.Contains(v.EngineNo));
            if (vinExists || engExists) return Conflict("Some VIN/EngineNo already exist.");

            var gr = new GoodsReceipt
            {
                GrNo = NewNo("GR"),
                PoId = req.PoId,
                ReceiptDate = req.ReceiptDate,
                WarehouseId = req.WarehouseId,
                CreatedBy = CurrentUserId(),
                CreatedAt = DateTime.UtcNow
            };
            await _db.GoodsReceipts.AddAsync(gr);
            await _db.SaveChangesAsync(); // need GrId

            var now = DateTime.UtcNow;
            var movedBy = CurrentUserId();

            foreach (var v in req.Vehicles)
            {
                // create vehicle
                var veh = new Vehicle
                {
                    ModelId = v.ModelId,
                    Vin = v.Vin,
                    EngineNo = v.EngineNo,
                    Color = v.Color,
                    Year = v.Year,
                    Status = "IN_STOCK",
                    CurrentWarehouseId = req.WarehouseId,
                    AcquiredAt = now,
                    UpdatedAt = now
                };
                await _db.Vehicles.AddAsync(veh);
                await _db.SaveChangesAsync(); // need VehicleId

                // gri
                var gri = new GoodsReceiptItem
                {
                    GrId = gr.GrId,
                    VehicleId = veh.VehicleId,
                    LandedCost = v.LandedCost
                };
                await _db.GoodsReceiptItems.AddAsync(gri);

                // inventory move
                var mv = new InventoryMove
                {
                    VehicleId = veh.VehicleId,
                    FromWarehouseId = null,
                    ToWarehouseId = req.WarehouseId,
                    Reason = "RECEIVE",
                    MovedAt = now,
                    MovedBy = movedBy
                };
                await _db.InventoryMoves.AddAsync(mv);
            }

            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = gr.GrId }, new { grId = gr.GrId, gr.GrNo });
        }
    }
}
