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
    public class QuotationsController : ControllerBase
    {
        private readonly ShowroomDbContext _db;
        public QuotationsController(ShowroomDbContext db) => _db = db;

        private const string QuoteSent    = "SENT";
        private const string QuoteConfirm = "CONFIRMED";

        private const string VehInStock   = "IN_STOCK";
        private const string VehAllocated = "ALLOCATED";

        private const string AllotReserved = "RESERVED";

        private static string NewDocNo(string prefix)
            => $"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmssfff}";

        [HttpPost]
        public async Task<ActionResult<QuotationDto>> Create(QuotationCreateRequest req)
        {
            if (req.Items == null || req.Items.Count == 0)
                return BadRequest("Quotation must have at least one item.");

            decimal subtotal = 0;
            foreach (var it in req.Items)
                subtotal += it.UnitPrice * it.Qty;
            var grand = subtotal - req.Discount + req.Tax;

            var q = new Quotation
            {
                QuoteNo    = NewDocNo("Q"),
                CustomerId = req.CustomerId,
                QuoteDate  = DateOnly.FromDateTime(DateTime.UtcNow), // <— FIX
                Status     = QuoteSent,
                Subtotal   = subtotal,
                Discount   = req.Discount,
                Tax        = req.Tax,
                GrandTotal = grand,
            };
            await _db.Quotations.AddAsync(q);
            await _db.SaveChangesAsync();

            var items = new List<QuotationItem>();
            foreach (var it in req.Items)
            {
                items.Add(new QuotationItem
                {
                    QuoteId   = q.QuoteId,
                    ModelId   = it.ModelId,
                    Qty       = it.Qty,
                    UnitPrice = it.UnitPrice,
                    LineTotal = it.UnitPrice * it.Qty
                });
            }
            await _db.QuotationItems.AddRangeAsync(items);
            await _db.SaveChangesAsync();

            var dto = new QuotationDto
            {
                QuoteId    = q.QuoteId,
                QuoteNo    = q.QuoteNo,
                CustomerId = q.CustomerId,
                QuoteDate  = q.QuoteDate,          // <— FIX (DateOnly → DateOnly)
                Status     = q.Status,
                Subtotal   = q.Subtotal,
                Discount   = q.Discount,
                Tax        = q.Tax,
                GrandTotal = q.GrandTotal,
                Items      = items.Select(x => new QuotationItemDto
                {
                    QuoteItemId = x.QuoteItemId,
                    QuoteId     = x.QuoteId,
                    ModelId     = x.ModelId,
                    Qty         = x.Qty,
                    UnitPrice   = x.UnitPrice,
                    LineTotal   = x.LineTotal ?? (x.UnitPrice * x.Qty)
                }).ToList()
            };
            return Ok(dto);
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<QuotationDto>> Get(long id)
        {
            var q = await _db.Quotations.FirstOrDefaultAsync(x => x.QuoteId == id);
            if (q == null) return NotFound();

            var items = await _db.QuotationItems
                                 .Where(i => i.QuoteId == id)
                                 .AsNoTracking()
                                 .ToListAsync();

            return Ok(new QuotationDto
            {
                QuoteId    = q.QuoteId,
                QuoteNo    = q.QuoteNo,
                CustomerId = q.CustomerId,
                QuoteDate  = q.QuoteDate,          // <— FIX
                Status     = q.Status,
                Subtotal   = q.Subtotal,
                Discount   = q.Discount,
                Tax        = q.Tax,
                GrandTotal = q.GrandTotal,
                Items      = items.Select(x => new QuotationItemDto
                {
                    QuoteItemId = x.QuoteItemId,
                    QuoteId     = x.QuoteId,
                    ModelId     = x.ModelId,
                    Qty         = x.Qty,
                    UnitPrice   = x.UnitPrice,
                    LineTotal   = x.LineTotal ?? (x.UnitPrice * x.Qty)
                }).ToList()
            });
        }

        [HttpPost("{id:long}/confirm")]
        public async Task<ActionResult<object>> Confirm(long id, [FromBody] QuotationConfirmOptions? options = null)
        {
            options ??= new QuotationConfirmOptions();
            var now = DateTime.UtcNow;

            var q = await _db.Quotations.FirstOrDefaultAsync(x => x.QuoteId == id);
            if (q == null) return NotFound();
            if (string.Equals(q.Status, QuoteConfirm, StringComparison.OrdinalIgnoreCase))
                return Conflict("Quotation is already confirmed.");

            var qItems = await _db.QuotationItems.Where(i => i.QuoteId == id).ToListAsync();
            if (qItems.Count == 0) return BadRequest("Quotation has no items.");

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var so = new SalesOrder
                {
                    SoNo        = NewDocNo("SO"),
                    CustomerId  = q.CustomerId,
                    OrderDate   = DateOnly.FromDateTime(DateTime.UtcNow), // <— FIX
                    Status      = "CONFIRMED",
                    Subtotal    = q.Subtotal,
                    Discount    = q.Discount,
                    Tax         = q.Tax,
                    GrandTotal  = q.GrandTotal
                };
                await _db.SalesOrders.AddAsync(so);
                await _db.SaveChangesAsync();

                if (options.AutoAllocateVehicles)
                {
                    foreach (var gi in qItems)
                    {
                        var available = await _db.Vehicles
                            .Where(v => v.ModelId == gi.ModelId && v.Status == VehInStock)
                            .OrderBy(v => v.VehicleId)
                            .Take(gi.Qty)
                            .ToListAsync();

                        if (available.Count < gi.Qty)
                            return Conflict($"Insufficient stock for model_id={gi.ModelId}. Need {gi.Qty}, have {available.Count}.");

                        foreach (var v in available)
                        {
                            await _db.SalesOrderItems.AddAsync(new SalesOrderItem
                            {
                                SoId      = so.SoId,
                                VehicleId = v.VehicleId,
                                SellPrice = gi.UnitPrice,
                                Discount  = 0,
                                Tax       = 0,
                                LineTotal = gi.UnitPrice
                            });

                            v.Status    = VehAllocated;
                            v.UpdatedAt = now;

                            var allot = await _db.Allotments.FirstOrDefaultAsync(a => a.VehicleId == v.VehicleId);
                            if (allot == null)
                            {
                                await _db.Allotments.AddAsync(new Allotment
                                {
                                    SoId       = so.SoId,
                                    VehicleId  = v.VehicleId,
                                    ReservedAt = now,
                                    Status     = AllotReserved
                                });
                            }
                            else
                            {
                                allot.SoId       = so.SoId;
                                allot.ReservedAt = now;
                                allot.Status     = AllotReserved;
                                _db.Allotments.Update(allot);
                            }
                        }
                    }
                }

                q.Status = QuoteConfirm;
                _db.Quotations.Update(q);

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new { soId = so.SoId, soNo = so.SoNo });
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
