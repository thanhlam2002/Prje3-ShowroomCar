using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShowroomCar.Application.Dtos;
using ShowroomCar.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Authorization;

namespace ShowroomCar.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "RequireEmployee")]
    public class InvoicesController : ControllerBase
    {
        private readonly ShowroomDbContext _context;
        public InvoicesController(ShowroomDbContext context) => _context = context;

        private static string NewNo(string prefix) => $"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        private static decimal R2(decimal x) => Math.Round(x, 2, MidpointRounding.AwayFromZero);

        // GET: api/invoices
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetAll()
        {
            var invoices = await _context.Invoices
                .AsNoTracking()
                .Include(i => i.Customer)
                .Include(i => i.InvoiceItems).ThenInclude(it => it.Vehicle)
                .ProjectToType<InvoiceDto>()
                .ToListAsync();

            return Ok(invoices);
        }

        // GET: api/invoices/{id}
        [HttpGet("{id:long}")]
        public async Task<ActionResult<InvoiceDto>> GetById(long id)
        {
            var entity = await _context.Invoices
                .AsNoTracking()
                .Include(i => i.Customer)
                .Include(i => i.InvoiceItems).ThenInclude(it => it.Vehicle)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);

            if (entity == null) return NotFound();
            return Ok(entity.Adapt<InvoiceDto>());
        }

        // ---------- DTO tạo hoá đơn (để đảm bảo có VehicleId & số tiền trên dòng) ----------
        public class InvoiceCreateItem
        {
            public long VehicleId { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal Discount { get; set; }
            public decimal Tax { get; set; }
        }
        public class InvoiceCreateRequest
        {
            public long CustomerId { get; set; }
            public List<InvoiceCreateItem> Items { get; set; } = new();
        }

        // POST: api/invoices  (tạo thủ công, KHÁC với issue-from-so)
        [HttpPost]
        public async Task<ActionResult<InvoiceDto>> Create([FromBody] InvoiceCreateRequest req)
        {
            if (req.Items == null || req.Items.Count == 0)
                return BadRequest("Invoice must have at least one item.");

            var cust = await _context.Customers.AsNoTracking()
                         .FirstOrDefaultAsync(c => c.CustomerId == req.CustomerId);
            if (cust == null) return NotFound($"Customer #{req.CustomerId} not found.");

            // Kiểm tra vehicle tồn tại
            var vehicleIds = req.Items.Select(x => x.VehicleId).ToHashSet();
            var vehicles = await _context.Vehicles
                .Where(v => vehicleIds.Contains(v.VehicleId))
                .Select(v => v.VehicleId)
                .ToListAsync();
            if (vehicles.Count != vehicleIds.Count)
                return BadRequest("Some vehicles do not exist.");

            var inv = new Invoice
            {
                InvoiceNo = NewNo("INV"),
                CustomerId = req.CustomerId,
                InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Status = "ISSUED",
                CreatedAt = DateTime.UtcNow
            };

            // Tạo dòng hàng + tính tổng
            var items = new List<InvoiceItem>();
            decimal subtotal = 0, discount = 0, tax = 0;

            foreach (var x in req.Items)
            {
                var lineTotal = R2(x.UnitPrice - x.Discount + x.Tax);
                items.Add(new InvoiceItem
                {
                    VehicleId = x.VehicleId,
                    UnitPrice = R2(x.UnitPrice),
                    Discount = R2(x.Discount),
                    Tax = R2(x.Tax),
                    LineTotal = lineTotal
                });
                subtotal += R2(x.UnitPrice);
                discount += R2(x.Discount);
                tax += R2(x.Tax);
            }

            inv.Subtotal = R2(subtotal);
            inv.Discount = R2(discount);
            inv.Tax = R2(tax);
            inv.GrandTotal = R2(inv.Subtotal - inv.Discount + inv.Tax);

            _context.Invoices.Add(inv);
            await _context.SaveChangesAsync();           // cần InvoiceId

            // gắn FK invoice cho từng item rồi lưu
            foreach (var it in items) it.InvoiceId = inv.InvoiceId;
            await _context.InvoiceItems.AddRangeAsync(items);
            await _context.SaveChangesAsync();

            // map trả DTO đầy đủ
            var created = await _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.InvoiceItems).ThenInclude(it => it.Vehicle)
                .FirstAsync(i => i.InvoiceId == inv.InvoiceId);

            return CreatedAtAction(nameof(GetById), new { id = inv.InvoiceId }, created.Adapt<InvoiceDto>());
        }

        // PUT: api/invoices/{id}  (upsert items + tính lại tổng, chặn khi đã có allocation)
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] InvoiceCreateRequest req)
        {
            var inv = await _context.Invoices
                .Include(i => i.InvoiceItems)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);

            if (inv == null) return NotFound();

            // Không cho sửa nếu đã có phân bổ thanh toán
            var hasAlloc = await _context.PaymentAllocations.AnyAsync(a => a.InvoiceId == id);
            if (hasAlloc) return Conflict("Invoice has payment allocations; cannot modify.");

            if (req.Items == null || req.Items.Count == 0)
                return BadRequest("Invoice must have at least one item.");

            inv.CustomerId = req.CustomerId;

            // Xoá items cũ, thêm items mới
            _context.InvoiceItems.RemoveRange(inv.InvoiceItems);
            await _context.SaveChangesAsync();

            decimal subtotal = 0, discount = 0, tax = 0;
            var newItems = new List<InvoiceItem>();

            foreach (var x in req.Items)
            {
                var lineTotal = R2(x.UnitPrice - x.Discount + x.Tax);
                newItems.Add(new InvoiceItem
                {
                    InvoiceId = id,
                    VehicleId = x.VehicleId,
                    UnitPrice = R2(x.UnitPrice),
                    Discount = R2(x.Discount),
                    Tax = R2(x.Tax),
                    LineTotal = lineTotal
                });
                subtotal += R2(x.UnitPrice);
                discount += R2(x.Discount);
                tax += R2(x.Tax);
            }

            await _context.InvoiceItems.AddRangeAsync(newItems);

            inv.Subtotal = R2(subtotal);
            inv.Discount = R2(discount);
            inv.Tax = R2(tax);
            inv.GrandTotal = R2(inv.Subtotal - inv.Discount + inv.Tax);

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/invoices/{id} (chặn xoá nếu đã phân bổ thanh toán)
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var hasAlloc = await _context.PaymentAllocations.AnyAsync(a => a.InvoiceId == id);
            if (hasAlloc) return Conflict("Invoice has payment allocations; cannot delete.");

            var entity = await _context.Invoices.FindAsync(id);
            if (entity == null) return NotFound();

            _context.Invoices.Remove(entity);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/invoices/issue-from-so/{soId}
        [HttpPost("issue-from-so/{soId:long}")]
        public async Task<ActionResult<InvoiceDto>> IssueFromSaleOrder(long soId)
        {
            // 1) Load Sale Order
            var so = await _context.SalesOrders
                .Include(s => s.Customer)
                .Include(s => s.SalesOrderItems).ThenInclude(i => i.Vehicle)
                .FirstOrDefaultAsync(s => s.SoId == soId);

            if (so == null)
                return NotFound("Sale Order not found.");

            // 2) Kiểm tra trạng thái SO
            if (so.Status != "PENDING_PAYMENT" && so.Status != "COMPLETED")
                return Conflict("Invoice can only be issued for PENDING_PAYMENT or COMPLETED Sales Orders.");

            // 3) Kiểm tra xem SO đã có invoice chưa
            var existing = await _context.Invoices
                .FirstOrDefaultAsync(i => i.SoId == soId);
            if (existing != null)
                return Conflict("Invoice already issued for this Sales Order.");

            // 4) Tạo invoice từ SO
            var inv = new Invoice
            {
                InvoiceNo = $"INV-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                CustomerId = so.CustomerId,
                SoId = so.SoId,
                InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Status = "ISSUED",
                Subtotal = so.Subtotal,
                Discount = so.Discount,
                Tax = so.Tax,
                GrandTotal = so.GrandTotal,
                CreatedAt = DateTime.UtcNow
            };

            // Save invoice first
            _context.Invoices.Add(inv);
            await _context.SaveChangesAsync();

            // 5) Copy Items từ SO sang Invoice
            foreach (var item in so.SalesOrderItems)
            {
                await _context.InvoiceItems.AddAsync(new InvoiceItem
                {
                    InvoiceId = inv.InvoiceId,
                    VehicleId = item.VehicleId,
                    UnitPrice = item.SellPrice,
                    Discount = item.Discount ?? 0m,
                    Tax = item.Tax ?? 0m,
                    LineTotal = item.SellPrice - (item.Discount ?? 0m) + (item.Tax ?? 0m)
                });
            }

            await _context.SaveChangesAsync();

            // 6) Trả DTO đầy đủ
            var dto = await _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.InvoiceItems).ThenInclude(it => it.Vehicle)
                .FirstAsync(i => i.InvoiceId == inv.InvoiceId);

            return CreatedAtAction(nameof(GetById), new { id = inv.InvoiceId }, dto.Adapt<InvoiceDto>());
        }

    }
}
