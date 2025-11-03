using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShowroomCar.Application.Dtos;
using ShowroomCar.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Authorization;

namespace ShowroomCar.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "RequireAdmin")]
    public class PurchaseOrdersController : ControllerBase
    {
        private readonly ShowroomDbContext _db;
        public PurchaseOrdersController(ShowroomDbContext db) => _db = db;

        private static string NewNo(string pfx) => $"{pfx}-{DateTime.UtcNow:yyyyMMddHHmmssfff}";

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PurchaseOrderDto>>> List()
        {
            var data = await _db.PurchaseOrders
                .AsNoTracking()
                .Include(p => p.Supplier)
                .Include(p => p.PurchaseOrderItems)
                .OrderByDescending(p => p.PoId)
                .ToListAsync();

            var dtos = data.Select(p => new PurchaseOrderDto
            {
                PoId = p.PoId,
                PoNo = p.PoNo,
                SupplierId = p.SupplierId,
                SupplierName = p.Supplier.Name,
                OrderDate = p.OrderDate,
                Status = p.Status,
                TotalAmount = p.TotalAmount,
                Items = p.PurchaseOrderItems.Select(i => new PurchaseOrderItemDto
                {
                    PoItemId = i.PoItemId,
                    PoId = p.PoId,
                    ModelId = i.ModelId,
                    Qty = i.Qty,
                    UnitPrice = i.UnitPrice,
                    LineTotal = i.LineTotal
                }).ToList()
            });

            return Ok(dtos);
        }

        [HttpPost]
        public async Task<ActionResult<PurchaseOrderDto>> Create(PurchaseOrderCreateRequest req)
        {
            if (req.Items == null || req.Items.Count == 0)
                return BadRequest("PO must have at least one item.");

            var sup = await _db.Suppliers.FindAsync(req.SupplierId);
            if (sup == null) return NotFound("Supplier not found.");

            // validate models
            var modelIds = req.Items.Select(x => x.ModelId).Distinct().ToList();
            var existing = await _db.VehicleModels.Where(m => modelIds.Contains(m.ModelId))
                                                  .Select(m => m.ModelId).ToListAsync();
            if (existing.Count != modelIds.Count)
                return BadRequest("Some model IDs do not exist.");

            var po = new PurchaseOrder
            {
                PoNo = NewNo("PO"),
                SupplierId = req.SupplierId,
                Status = "APPROVED", // đơn giản: tạo là Approve luôn (đủ cho demo)
                OrderDate = req.OrderDate,
                TotalAmount = 0,
                CreatedBy = null,
                CreatedAt = DateTime.UtcNow
            };
            await _db.PurchaseOrders.AddAsync(po);
            await _db.SaveChangesAsync();

            decimal total = 0;
            var items = new List<PurchaseOrderItem>();
            foreach (var x in req.Items)
            {
                var lt = x.UnitPrice * x.Qty;
                items.Add(new PurchaseOrderItem
                {
                    PoId = po.PoId,
                    ModelId = x.ModelId,
                    Qty = x.Qty,
                    UnitPrice = x.UnitPrice,
                    LineTotal = lt
                });
                total += lt;
            }

            await _db.PurchaseOrderItems.AddRangeAsync(items);
            po.TotalAmount = total;
            _db.PurchaseOrders.Update(po);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = po.PoId }, new { poId = po.PoId, po.PoNo });
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<PurchaseOrderDto>> Get(long id)
        {
            var p = await _db.PurchaseOrders
                .AsNoTracking()
                .Include(x => x.Supplier)
                .Include(x => x.PurchaseOrderItems)
                .FirstOrDefaultAsync(x => x.PoId == id);

            if (p == null) return NotFound();

            return Ok(new PurchaseOrderDto
            {
                PoId = p.PoId,
                PoNo = p.PoNo,
                SupplierId = p.SupplierId,
                SupplierName = p.Supplier.Name,
                OrderDate = p.OrderDate,
                Status = p.Status,
                TotalAmount = p.TotalAmount,
                Items = p.PurchaseOrderItems.Select(i => new PurchaseOrderItemDto
                {
                    PoItemId = i.PoItemId,
                    PoId = i.PoId,
                    ModelId = i.ModelId,
                    Qty = i.Qty,
                    UnitPrice = i.UnitPrice,
                    LineTotal = i.LineTotal
                }).ToList()
            });
        }
    }
}
