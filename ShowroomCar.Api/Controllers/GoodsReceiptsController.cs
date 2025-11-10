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
                .Select(v => new { v.VehicleId, v.Vin, v.EngineNo })
                .ToListAsync();

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
                var poCheck = await _db.PurchaseOrders.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PoId == req.PoId.Value);
                if (poCheck == null) return BadRequest("PO not found.");
            }

            // VIN/Engine uniqueness pre-check
            var vins = req.Vehicles.Select(v => v.Vin).ToList();
            var engs = req.Vehicles.Select(v => v.EngineNo).ToList();
            if (await _db.Vehicles.AnyAsync(v => vins.Contains(v.Vin) || engs.Contains(v.EngineNo)))
                return Conflict("Some VIN/EngineNo already exist.");

            // ✅ Validate ModelId tồn tại trong database
            var modelIds = req.Vehicles.Select(v => v.ModelId).Distinct().ToList();
            var existingModels = await _db.VehicleModels
                .Where(m => modelIds.Contains(m.ModelId))
                .Select(m => m.ModelId)
                .ToListAsync();
            
            var missingModels = modelIds.Except(existingModels).ToList();
            if (missingModels.Any())
                return BadRequest($"Model IDs not found: {string.Join(", ", missingModels)}");

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var now = DateTime.UtcNow;
                var userId = CurrentUserId();

                // 1️⃣ Tạo phiếu nhập
                var gr = new GoodsReceipt
                {
                    GrNo = NewNo("GR"),
                    PoId = req.PoId,
                    ReceiptDate = req.ReceiptDate,
                    WarehouseId = req.WarehouseId,
                    CreatedBy = userId,
                    CreatedAt = now
                };
                await _db.GoodsReceipts.AddAsync(gr);
                await _db.SaveChangesAsync(); // cần GrId

                // 2️⃣ Tạo danh sách xe
                var vehicles = new List<Vehicle>();
                foreach (var v in req.Vehicles)
                {
                    vehicles.Add(new Vehicle
                    {
                        ModelId = v.ModelId,
                        Vin = v.Vin,
                        EngineNo = v.EngineNo,
                        Color = v.Color,
                        Year = v.Year,
                        Status = "UNDER_INSPECTION",
                        CurrentWarehouseId = req.WarehouseId,
                        AcquiredAt = now,
                        UpdatedAt = now
                    });
                }
                await _db.Vehicles.AddRangeAsync(vehicles);
                await _db.SaveChangesAsync(); // để có VehicleId

                // 3️⃣ Tạo GR Items và ServiceOrders
                var grItems = new List<GoodsReceiptItem>();
                var svcOrders = new List<ServiceOrder>();
                var baseTime = DateTime.UtcNow;
                var index = 0;

                foreach (var veh in vehicles)
                {
                    grItems.Add(new GoodsReceiptItem
                    {
                        GrId = gr.GrId,
                        VehicleId = veh.VehicleId,
                        LandedCost = req.Vehicles
                            .First(x => x.Vin == veh.Vin).LandedCost
                    });

                    // ✅ Tạo SvcNo unique bằng cách thêm index và GUID ngắn
                    var uniqueSuffix = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
                    var svcNo = $"SVC-INSP-{baseTime:yyyyMMddHHmmssfff}-{index:D3}-{uniqueSuffix}";
                    
                    svcOrders.Add(new ServiceOrder
                    {
                        SvcNo = svcNo,
                        VehicleId = veh.VehicleId,
                        ModelId = veh.ModelId, // ✅ Thêm ModelId từ Vehicle (đã validate tồn tại)
                        PoId = gr.PoId,        // ✅ Thêm PoId từ GR (nullable)
                        GrId = gr.GrId,        // ✅ Thêm GrId từ GR (nullable nhưng luôn có vì vừa tạo)
                        QuantityExpected = 1,  // ✅ Mỗi ServiceOrder cho 1 xe
                        ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow),
                        Status = "PLANNED",
                        Notes = "Initial inspection after goods receipt",
                        CreatedBy = userId,
                        CreatedAt = now
                    });
                    
                    index++;
                }

                await _db.GoodsReceiptItems.AddRangeAsync(grItems);
                await _db.ServiceOrders.AddRangeAsync(svcOrders);
                await _db.SaveChangesAsync();

                // 4️⃣ Cập nhật trạng thái PO
                if (gr.PoId.HasValue)
                {
                    var po = await _db.PurchaseOrders
                        .Include(p => p.PurchaseOrderItems)
                        .FirstOrDefaultAsync(p => p.PoId == gr.PoId.Value);

                    if (po != null)
                    {
                        var totalOrdered = po.PurchaseOrderItems.Sum(i => i.Qty);
                        var totalReceived = await _db.GoodsReceiptItems
                            .Where(i => i.Gr.PoId == po.PoId)
                            .CountAsync();

                        po.Status = totalReceived >= totalOrdered ? "CLOSED" : "RECEIVING";
                        _db.PurchaseOrders.Update(po);
                        await _db.SaveChangesAsync();
                    }
                }

                await tx.CommitAsync();

                return CreatedAtAction(nameof(Get), new { id = gr.GrId }, new
                {
                    grId = gr.GrId,
                    gr.GrNo,
                    Vehicles = vehicles.Count,
                    ServiceOrders = svcOrders.Count
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, $"Transaction failed: {ex.Message}");
            }
        }
    }
}
