using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShowroomCar.Application.Dtos;
using ShowroomCar.Infrastructure.Persistence.Entities;

namespace ShowroomCar.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "RequireEmployee")]
    public class SalesOrdersController : ControllerBase
    {
        private readonly ShowroomDbContext _db;
        public SalesOrdersController(ShowroomDbContext db) => _db = db;

        private static string NewNo(string pfx) => $"{pfx}-{DateTime.UtcNow:yyyyMMddHHmmssfff}";

        // GET: api/salesorders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SalesOrderDto>>> List()
        {
            var data = await _db.SalesOrders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.SalesOrderItems).ThenInclude(i => i.Vehicle)
                .OrderByDescending(o => o.SoId)
                .ToListAsync();

            var dto = data.Select(o => new SalesOrderDto
            {
                SoId = o.SoId,
                SoNo = o.SoNo,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer?.FullName ?? "",
                OrderDate = o.OrderDate,
                Status = o.Status,
                Subtotal = o.Subtotal,
                Discount = o.Discount,
                Tax = o.Tax,
                GrandTotal = o.GrandTotal,
                Items = o.SalesOrderItems.Select(i => new SalesOrderItemDto
                {
                    SoItemId = i.SoItemId,
                    VehicleId = i.VehicleId,
                    VehicleVin = i.Vehicle?.Vin ?? "",
                    SellPrice = i.SellPrice,
                    Discount = i.Discount ?? 0m,
                    Tax = i.Tax ?? 0m
                }).ToList()
            });

            return Ok(dto);
        }

        // GET: api/salesorders/{id}
        [HttpGet("{id:long}")]
        public async Task<ActionResult<SalesOrderDto>> Get(long id)
        {
            var o = await _db.SalesOrders
                .AsNoTracking()
                .Include(x => x.Customer)
                .Include(x => x.SalesOrderItems).ThenInclude(i => i.Vehicle)
                .FirstOrDefaultAsync(x => x.SoId == id);

            if (o == null) return NotFound();

            var dto = new SalesOrderDto
            {
                SoId = o.SoId,
                SoNo = o.SoNo,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer?.FullName ?? "",
                OrderDate = o.OrderDate,
                Status = o.Status,
                Subtotal = o.Subtotal,
                Discount = o.Discount,
                Tax = o.Tax,
                GrandTotal = o.GrandTotal,
                Items = o.SalesOrderItems.Select(i => new SalesOrderItemDto
                {
                    SoItemId = i.SoItemId,
                    VehicleId = i.VehicleId,
                    VehicleVin = i.Vehicle?.Vin ?? "",
                    SellPrice = i.SellPrice,
                    Discount = i.Discount ?? 0m,
                    Tax = i.Tax ?? 0m
                }).ToList()
            };

            return Ok(dto);
        }

        // POST: api/salesorders
        [HttpPost]
        public async Task<ActionResult<object>> Create([FromBody] SalesOrderCreateRequest req)
        {
            if (req.Items == null || req.Items.Count == 0)
                return BadRequest("Sales order must have at least one item.");

            var cust = await _db.Customers.FindAsync(req.CustomerId);
            if (cust == null) return NotFound("Customer not found.");

            var vehIds = req.Items.Select(x => x.VehicleId).Distinct().ToList();
            var vehicles = await _db.Vehicles
                .Where(v => vehIds.Contains(v.VehicleId))
                .ToListAsync();

            if (vehicles.Count != vehIds.Count)
                return BadRequest("Some vehicles not found.");

            if (vehicles.Any(v => !string.Equals(v.Status, "IN_STOCK", StringComparison.OrdinalIgnoreCase)))
                return Conflict("All vehicles must be IN_STOCK.");

            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var so = new SalesOrder
                {
                    SoNo = NewNo("SO"),
                    CustomerId = req.CustomerId,
                    OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    Status = "DRAFT",
                    Subtotal = req.Items.Sum(x => x.SellPrice),
                    Discount = req.Items.Sum(x => x.Discount),
                    Tax = req.Items.Sum(x => x.Tax),
                    GrandTotal = req.Items.Sum(x => x.SellPrice) - req.Items.Sum(x => x.Discount) + req.Items.Sum(x => x.Tax)
                };
                await _db.SalesOrders.AddAsync(so);
                await _db.SaveChangesAsync();

                var items = req.Items.Select(x => new SalesOrderItem
                {
                    SoId = so.SoId,
                    VehicleId = x.VehicleId,
                    SellPrice = x.SellPrice,
                    Discount = x.Discount,
                    Tax = x.Tax
                }).ToList();

                await _db.SalesOrderItems.AddRangeAsync(items);

                // Giữ xe
                foreach (var v in vehicles)
                {
                    v.Status = "ALLOCATED";
                    v.UpdatedAt = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return CreatedAtAction(nameof(Get), new { id = so.SoId }, new { soId = so.SoId, so.SoNo });
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // ✅ POST: api/salesorders/{id}/confirm
        [HttpPost("{id:long}/confirm")]
        public async Task<ActionResult<object>> Confirm(long id)
        {
            var so = await _db.SalesOrders
                .Include(s => s.SalesOrderItems)
                .ThenInclude(i => i.Vehicle)
                .FirstOrDefaultAsync(s => s.SoId == id);

            if (so == null) return NotFound();
            if (so.Status == "COMPLETED")
                return Conflict("Order already confirmed.");

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // Xe -> SOLD
                foreach (var item in so.SalesOrderItems)
                {
                    item.Vehicle.Status = "SOLD";
                    item.Vehicle.UpdatedAt = DateTime.UtcNow;
                }

                // Sinh hóa đơn
                var inv = new Invoice
                {
                    InvoiceNo = NewNo("INV"),
                    CustomerId = so.CustomerId,
                    SoId = so.SoId,
                    InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    Status = "ISSUED",
                    Subtotal = so.Subtotal,
                    Discount = so.Discount,
                    Tax = so.Tax,
                    GrandTotal = so.GrandTotal
                };
                await _db.Invoices.AddAsync(inv);
                await _db.SaveChangesAsync();

                foreach (var item in so.SalesOrderItems)
                {
                    await _db.InvoiceItems.AddAsync(new InvoiceItem
                    {
                        InvoiceId = inv.InvoiceId,
                        VehicleId = item.VehicleId,
                        UnitPrice = item.SellPrice,
                        Discount = item.Discount ?? 0m,
                        Tax = item.Tax ?? 0m
                    });
                }

                so.Status = "COMPLETED";
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new
                {
                    message = "Sales order confirmed and invoice created.",
                    soId = so.SoId,
                    invoiceNo = inv.InvoiceNo
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return BadRequest(ex.Message);
            }
        }

        // DELETE: api/salesorders/{id}
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var o = await _db.SalesOrders
                .Include(x => x.SalesOrderItems)
                .FirstOrDefaultAsync(x => x.SoId == id);

            if (o == null) return NotFound();

            _db.SalesOrderItems.RemoveRange(o.SalesOrderItems);
            _db.SalesOrders.Remove(o);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
