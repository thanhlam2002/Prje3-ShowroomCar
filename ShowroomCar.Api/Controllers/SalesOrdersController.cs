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
        // Luồng:
        // - Khách đã gửi request: FE gửi kèm RequestId + 1 item chứa VehicleId đúng xe đã reserve.
        // - Khách mua trực tiếp: không có RequestId, nhân viên chọn VehicleId từ kho (IN_STOCK / RESERVED).
        [HttpPost]
        public async Task<ActionResult<object>> Create([FromBody] SalesOrderCreateRequest req)
        {
            if (req.Items == null || req.Items.Count == 0)
                return BadRequest("Sales order must have at least one item.");

            var cust = await _db.Customers.FindAsync(req.CustomerId);
            if (cust == null) return NotFound("Customer not found.");

            VehicleRequest? vReq = null;

            // ===========================================================
            // 1) Trường hợp có RequestId → validate customer + validate vehicle
            // ===========================================================
            if (req.RequestId.HasValue)
            {
                vReq = await _db.VehicleRequests
                    .FirstOrDefaultAsync(r => r.RequestId == req.RequestId.Value);

                if (vReq == null)
                    return BadRequest("Vehicle request not found.");

                if (vReq.CustomerId.HasValue && vReq.CustomerId.Value != req.CustomerId)
                    return BadRequest("Vehicle request does not belong to this customer.");

                // Nếu request được gán sẵn 1 xe thì SO phải dùng đúng xe đó
                if (vReq.VehicleId.HasValue)
                {
                    var vehIdsFromReq = req.Items.Select(i => i.VehicleId).Distinct().ToList();

                    if (vehIdsFromReq.Count != 1 || vehIdsFromReq[0] != vReq.VehicleId.Value)
                        return BadRequest("Sales order must use the vehicle reserved in the request.");
                }
            }

            // ===========================================================
            // 2) Validate danh sách xe
            // ===========================================================
            var vehIds = req.Items.Select(x => x.VehicleId).Distinct().ToList();
            var vehicles = await _db.Vehicles
                .Where(v => vehIds.Contains(v.VehicleId))
                .ToListAsync();

            if (vehicles.Count != vehIds.Count)
                return BadRequest("Some vehicles not found.");

            // Chỉ IN_STOCK hoặc RESERVED mới được bán
            if (vehicles.Any(v => v.Status != "IN_STOCK" && v.Status != "RESERVED"))
                return Conflict("Vehicle must be IN_STOCK or RESERVED to create a Sales Order.");

            // Xe RESERVED → chỉ đúng customer mới được mua
            foreach (var v in vehicles.Where(v => v.Status == "RESERVED"))
            {
                if (v.ReservedForCustomerId != req.CustomerId)
                    return Conflict($"Vehicle {v.VehicleId} is reserved for another customer.");
            }

            // ===========================================================
            // 3) Tạo SaleOrder
            // ===========================================================
            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var now = DateTime.UtcNow;

                var so = new SalesOrder
                {
                    SoNo = NewNo("SO"),
                    CustomerId = req.CustomerId,
                    RequestId = req.RequestId,   // <-- GẮN REQUEST VÀO SALE ORDER
                    OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    Status = "DRAFT",
                    Subtotal = req.Items.Sum(x => x.SellPrice),
                    Discount = req.Items.Sum(x => x.Discount),
                    Tax = req.Items.Sum(x => x.Tax),
                    GrandTotal = req.Items.Sum(x => x.SellPrice)
                                 - req.Items.Sum(x => x.Discount)
                                 + req.Items.Sum(x => x.Tax)
                };

                await _db.SalesOrders.AddAsync(so);
                await _db.SaveChangesAsync();

                // ===========================================================
                // 4) Tạo SalesOrder Items
                // ===========================================================
                var items = req.Items.Select(x => new SalesOrderItem
                {
                    SoId = so.SoId,
                    VehicleId = x.VehicleId,
                    SellPrice = x.SellPrice,
                    Discount = x.Discount,
                    Tax = x.Tax
                }).ToList();

                await _db.SalesOrderItems.AddRangeAsync(items);

                // ===========================================================
                // 5) Update trạng thái xe → ALLOCATED (đã giữ cho SO này)
                // ===========================================================
                foreach (var v in vehicles)
                {
                    v.Status = "ALLOCATED";
                    v.UpdatedAt = now;
                }

                // ===========================================================
                // 6) Nếu tạo từ request → cập nhật trạng thái request
                // ===========================================================
                if (vReq != null)
                {
                    vReq.SoId = so.SoId;
                    vReq.Status = "SO_CREATED";
                    vReq.ProcessedAt = now;
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return CreatedAtAction(nameof(Get), new { id = so.SoId }, new
                {
                    soId = so.SoId,
                    so.SoNo
                });
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }


        // ✅ POST: api/salesorders/{id}/confirm (luồng cũ – confirm + sinh invoice ngay)
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

        [HttpGet("{id:long}/contract")]
        [AllowAnonymous]
        public async Task<IActionResult> GetContract(long id)
        {
            var so = await _db.SalesOrders
                .Include(s => s.Customer)
                .Include(s => s.SalesOrderItems).ThenInclude(i => i.Vehicle)
                .FirstOrDefaultAsync(s => s.SoId == id);

            if (so == null) return NotFound();

            return Ok(new SalesOrderContractDto
            {
                SoId = so.SoId,
                SoNo = so.SoNo,
                CustomerName = so.Customer.FullName,
                CustomerPhone = so.Customer.Phone,
                CustomerEmail = so.Customer.Email,
                VehicleVin = so.SalesOrderItems.First().Vehicle.Vin,
                VehicleModel = so.SalesOrderItems.First().Vehicle.ModelId.ToString(),
                Price = so.GrandTotal,
                Terms = "Điều khoản hợp đồng..."
            });
        }

        [HttpPost("{id:long}/customer-confirm")]
        [AllowAnonymous]
        public async Task<IActionResult> CustomerConfirm(long id)
        {
            var so = await _db.SalesOrders
                .Include(s => s.SalesOrderItems).ThenInclude(i => i.Vehicle)
                .FirstOrDefaultAsync(s => s.SoId == id);

            if (so == null) return NotFound();

            if (so.Status != "DRAFT")
                return BadRequest("Sales order is not in DRAFT.");

            so.Status = "PENDING_PAYMENT";
            so.ContractConfirmedAt = DateTime.UtcNow;

            foreach (var v in so.SalesOrderItems.Select(i => i.Vehicle))
            {
                v.Status = "PENDING_PAYMENT";
                v.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            return Ok(new { message = "Contract confirmed. Waiting for payment." });
        }

        [HttpPut("{id:long}/confirm-payment")]
        public async Task<IActionResult> ConfirmPayment(long id)
        {
            var so = await _db.SalesOrders
                .Include(s => s.SalesOrderItems).ThenInclude(i => i.Vehicle)
                .FirstOrDefaultAsync(s => s.SoId == id);

            if (so == null) return NotFound();

            if (so.Status != "PENDING_PAYMENT")
                return BadRequest("SO has not been confirmed by customer.");

            // Xe -> SOLD
            foreach (var v in so.SalesOrderItems.Select(i => i.Vehicle))
            {
                v.Status = "SOLD";
                v.UpdatedAt = DateTime.UtcNow;
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
                _db.InvoiceItems.Add(new InvoiceItem
                {
                    InvoiceId = inv.InvoiceId,
                    VehicleId = item.VehicleId,
                    UnitPrice = item.SellPrice,
                    Discount = item.Discount ?? 0,
                    Tax = item.Tax ?? 0
                });
            }

            so.Status = "COMPLETED";
            await _db.SaveChangesAsync();

            return Ok(new { message = "Payment confirmed. Invoice created.", invoiceNo = inv.InvoiceNo });
        }
    }
}
