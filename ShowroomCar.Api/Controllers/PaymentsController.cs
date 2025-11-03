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
    public class PaymentsController : ControllerBase
    {
        private readonly ShowroomDbContext _db;
        public PaymentsController(ShowroomDbContext db) => _db = db;

        private const string InvIssued      = "ISSUED";
        private const string InvPaidPartial = "PAID_PARTIAL";
        private const string InvPaidFull    = "PAID_FULL";

        private static string NewNo(string prefix) => $"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmssfff}";

        // ----- Helpers -------------------------------------------------------
        private static decimal Round2(decimal x) => Math.Round(x, 2, MidpointRounding.AwayFromZero);

        private async Task<decimal> GetInvoiceAllocated(long invoiceId, CancellationToken ct)
        {
            return await _db.PaymentAllocations
                .Where(a => a.InvoiceId == invoiceId)
                .SumAsync(a => (decimal?)a.AmountApplied, ct) ?? 0m;
        }

        private async Task<decimal> GetPaymentAllocated(long paymentId, CancellationToken ct)
        {
            return await _db.PaymentAllocations
                .Where(a => a.PaymentId == paymentId)
                .SumAsync(a => (decimal?)a.AmountApplied, ct) ?? 0m;
        }

        private async Task UpdateInvoiceStatusByBalance(Invoice inv, CancellationToken ct)
        {
            var allocated = await GetInvoiceAllocated(inv.InvoiceId, ct);
            var due = Round2(inv.GrandTotal - allocated);

            if (due <= 0m)      inv.Status = InvPaidFull;
            else if (allocated > 0m) inv.Status = InvPaidPartial;
            else                 inv.Status = InvIssued;

            _db.Invoices.Update(inv);
        }

        private async Task<PaymentSummaryDto> BuildPaymentSummary(long paymentId, CancellationToken ct)
        {
            var p = await _db.Payments.FirstAsync(x => x.PaymentId == paymentId, ct);
            var allocated = await GetPaymentAllocated(paymentId, ct);
            return new PaymentSummaryDto
            {
                PaymentId  = p.PaymentId,
                ReceiptNo  = p.ReceiptNo,
                CustomerId = p.CustomerId,
                PaymentDate= p.PaymentDate,
                Method     = p.Method,
                Amount     = p.Amount,
                Allocated  = Round2(allocated),
                Remaining  = Round2(p.Amount - allocated)
            };
        }

        // ----- API -----------------------------------------------------------

        // Tạo phiếu thu
        [HttpPost]
        public async Task<ActionResult<PaymentSummaryDto>> Create(PaymentCreateRequest req, CancellationToken ct)
        {
            if (req.Amount <= 0) return BadRequest("Amount must be > 0.");

            var custExists = await _db.Customers.AnyAsync(c => c.CustomerId == req.CustomerId, ct);
            if (!custExists) return NotFound($"Customer #{req.CustomerId} not found.");

            var pay = new Payment
            {
                ReceiptNo   = NewNo("RCPT"),
                CustomerId  = req.CustomerId,
                PaymentDate = req.PaymentDate,
                Method      = req.Method,
                Amount      = Round2(req.Amount),
                Notes       = req.Notes
            };
            _db.Payments.Add(pay);
            await _db.SaveChangesAsync(ct);

            return Ok(await BuildPaymentSummary(pay.PaymentId, ct));
        }

        // Phân bổ 1 phiếu thu vào nhiều hóa đơn
        [HttpPost("{paymentId:long}/allocate")]
        public async Task<ActionResult<PaymentSummaryDto>> Allocate(long paymentId, PaymentAllocateRequest req, CancellationToken ct)
        {
            if (req.Allocations == null || req.Allocations.Count == 0)
                return BadRequest("Allocations is empty.");

            var pay = await _db.Payments.FirstOrDefaultAsync(p => p.PaymentId == paymentId, ct);
            if (pay == null) return NotFound($"Payment #{paymentId} not found.");

            var remaining = Round2(pay.Amount - await GetPaymentAllocated(paymentId, ct));
            var totalAllocReq = Round2(req.Allocations.Sum(x => x.Amount));
            if (totalAllocReq <= 0) return BadRequest("Total allocation must be > 0.");
            if (totalAllocReq > remaining) return Conflict($"Allocation exceeds payment remaining ({remaining:N2}).");

            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                foreach (var line in req.Allocations)
                {
                    if (line.Amount <= 0) return BadRequest("Allocation amount must be > 0.");

                    var inv = await _db.Invoices.FirstOrDefaultAsync(i => i.InvoiceId == line.InvoiceId, ct);
                    if (inv == null) return NotFound($"Invoice #{line.InvoiceId} not found.");
                    if (inv.CustomerId != pay.CustomerId)
                        return Conflict($"Invoice #{inv.InvoiceId} belongs to another customer.");

                    var allocated = await GetInvoiceAllocated(inv.InvoiceId, ct);
                    var due = Round2(inv.GrandTotal - allocated);
                    if (line.Amount > due)
                        return Conflict($"Alloc {line.Amount:N2} > invoice due {due:N2} for invoice #{inv.InvoiceNo}.");

                    // tạo allocation
                    var alloc = new PaymentAllocation
                    {
                        PaymentId     = pay.PaymentId,
                        InvoiceId     = inv.InvoiceId,
                        AmountApplied = Round2(line.Amount)
                    };
                    _db.PaymentAllocations.Add(alloc);

                    // cập nhật trạng thái hóa đơn theo số dư
                    await UpdateInvoiceStatusByBalance(inv, ct);

                    remaining = Round2(remaining - line.Amount);
                    if (remaining < 0) remaining = 0;
                }

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return Ok(await BuildPaymentSummary(paymentId, ct));
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        // Xem chi tiết 1 phiếu thu
        [HttpGet("{paymentId:long}")]
        public async Task<ActionResult<object>> Get(long paymentId, CancellationToken ct)
        {
            var p = await _db.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PaymentId == paymentId, ct);
            if (p == null) return NotFound();

            var allocs = await _db.PaymentAllocations
                .Where(a => a.PaymentId == paymentId)
                .Join(_db.Invoices, a => a.InvoiceId, i => i.InvoiceId,
                    (a, i) => new { a.AmountApplied, i.InvoiceId, i.InvoiceNo })
                .ToListAsync(ct);

            var allocated = allocs.Sum(x => x.AmountApplied);
            var dto = new
            {
                p.PaymentId,
                p.ReceiptNo,
                p.CustomerId,
                p.PaymentDate,
                p.Method,
                p.Amount,
                Allocated = Round2(allocated),
                Remaining = Round2(p.Amount - allocated),
                Allocations = allocs.Select(x => new { x.InvoiceId, x.InvoiceNo, Amount = x.AmountApplied })
            };
            return Ok(dto);
        }
    }
}
