using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShowroomCar.Infrastructure.Persistence.Entities;
using ShowroomCar.Application.Dtos;
using Mapster;

namespace ShowroomCar.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoicesController : ControllerBase
    {
        private readonly ShowroomDbContext _context;

        public InvoicesController(ShowroomDbContext context)
        {
            _context = context;
        }

        // GET: api/invoices
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetAll()
        {
            var invoices = await _context.Invoices
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
                .Include(i => i.Customer)
                .Include(i => i.InvoiceItems).ThenInclude(it => it.Vehicle)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);

            if (entity == null)
                return NotFound();

            return Ok(entity.Adapt<InvoiceDto>());
        }

        // POST: api/invoices
        [HttpPost]
        public async Task<ActionResult<InvoiceDto>> Create([FromBody] InvoiceDto dto)
        {
            var entity = dto.Adapt<Infrastructure.Persistence.Entities.Invoice>();
            entity.InvoiceDate = DateOnly.FromDateTime(DateTime.Now);
            entity.Status = "ISSUED";
            entity.Subtotal = dto.Items.Sum(i => i.UnitPrice);
            entity.Discount = dto.Items.Sum(i => i.Discount);
            entity.Tax = dto.Items.Sum(i => i.Tax);
            entity.GrandTotal = entity.Subtotal - entity.Discount + entity.Tax;

            _context.Invoices.Add(entity);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = entity.InvoiceId }, entity.Adapt<InvoiceDto>());
        }

        // PUT: api/invoices/{id}
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] InvoiceDto dto)
        {
            var entity = await _context.Invoices
                .Include(i => i.InvoiceItems)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);

            if (entity == null)
                return NotFound();

            dto.Adapt(entity);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/invoices/{id}
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var entity = await _context.Invoices.FindAsync(id);
            if (entity == null)
                return NotFound();

            _context.Invoices.Remove(entity);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
