using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShowroomCar.Infrastructure.Persistence.Entities;
using ShowroomCar.Application.Dtos;
using Mapster;

namespace ShowroomCar.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly ShowroomDbContext _context;

        public CustomersController(ShowroomDbContext context)
        {
            _context = context;
        }

        // GET: api/customers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAll()
        {
            var customers = await _context.Customers
                .ProjectToType<CustomerDto>()
                .ToListAsync();

            return Ok(customers);
        }

        // GET: api/customers/{id}
        [HttpGet("{id:long}")]
        public async Task<ActionResult<CustomerDto>> GetById(long id)
        {
            var entity = await _context.Customers.FindAsync(id);
            if (entity == null)
                return NotFound();

            return Ok(entity.Adapt<CustomerDto>());
        }

        // POST: api/customers
        [HttpPost]
        public async Task<ActionResult<CustomerDto>> Create([FromBody] CustomerDto dto)
        {
            var entity = dto.Adapt<Infrastructure.Persistence.Entities.Customer>();
            entity.CreatedAt = DateTime.Now;
            entity.UpdatedAt = DateTime.Now;

            _context.Customers.Add(entity);
            await _context.SaveChangesAsync();

            var result = entity.Adapt<CustomerDto>();
            return CreatedAtAction(nameof(GetById), new { id = entity.CustomerId }, result);
        }

        // PUT: api/customers/{id}
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] CustomerDto dto)
        {
            var entity = await _context.Customers.FindAsync(id);
            if (entity == null)
                return NotFound();

            dto.Adapt(entity); // Mapster cập nhật field
            entity.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/customers/{id}
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var entity = await _context.Customers.FindAsync(id);
            if (entity == null)
                return NotFound();

            _context.Customers.Remove(entity);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
