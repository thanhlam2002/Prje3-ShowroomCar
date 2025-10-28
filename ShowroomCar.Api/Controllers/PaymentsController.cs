using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShowroomCar.Infrastructure.Persistence.Entities;
using ShowroomCar.Application.Dtos;
using Mapster;

namespace ShowroomCar.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly ShowroomDbContext _context;

        public PaymentsController(ShowroomDbContext context)
        {
            _context = context;
        }

        // GET: api/payments
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PaymentDto>>> GetAll()
        {
            var payments = await _context.Payments
                .Include(p => p.Customer)
                .Include(p => p.PaymentAllocations).ThenInclude(a => a.Invoice)
                .ProjectToType<PaymentDto>()
                .ToListAsync();

            return Ok(payments);
        }

        // GET: api/payments/{id}
        [HttpGet("{id:long}")]
        public async Task<ActionResult<PaymentDto>> GetById(long id)
        {
            var entity = await _context.Payments
                .Include(p => p.Customer)
                .Include(p => p.PaymentAllocations).ThenInclude(a => a.Invoice)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (entity == null)
                return NotFound();

            return Ok(entity.Adapt<PaymentDto>());
        }

        // POST: api/payments
        [HttpPost]
        public async Task<ActionResult<PaymentDto>> Create([FromBody] PaymentDto dto)
        {
            var entity = dto.Adapt<Infrastructure.Persistence.Entities.Payment>();
            entity.PaymentDate = DateOnly.FromDateTime(DateTime.Now);

            _context.Payments.Add(entity);
            await _context.SaveChangesAsync();

            // Lưu các allocation (phân bổ vào hóa đơn)
            foreach (var allocDto in dto.Allocations)
            {
                var alloc = new Infrastructure.Persistence.Entities.PaymentAllocation
                {
                    PaymentId = entity.PaymentId,
                    InvoiceId = allocDto.InvoiceId,
                    AmountApplied = allocDto.AmountApplied
                };
                _context.PaymentAllocations.Add(alloc);

                // Cập nhật trạng thái hóa đơn
                var invoice = await _context.Invoices.FindAsync(allocDto.InvoiceId);
                if (invoice != null)
                {
                    decimal totalPaid = await _context.PaymentAllocations
                        .Where(a => a.InvoiceId == invoice.InvoiceId)
                        .SumAsync(a => a.AmountApplied);

                    if (totalPaid >= invoice.GrandTotal)
                        invoice.Status = "PAID";
                    else
                        invoice.Status = "PARTIAL";
                }
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = entity.PaymentId }, entity.Adapt<PaymentDto>());
        }

        // DELETE: api/payments/{id}
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var entity = await _context.Payments.FindAsync(id);
            if (entity == null)
                return NotFound();

            _context.Payments.Remove(entity);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
