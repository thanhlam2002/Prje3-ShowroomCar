using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShowroomCar.Infrastructure.Persistence.Entities;
using ShowroomCar.Application.Dtos;
using Mapster;

namespace ShowroomCar.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalesOrdersController : ControllerBase
    {
        private readonly ShowroomDbContext _context;

        public SalesOrdersController(ShowroomDbContext context)
        {
            _context = context;
        }

        // GET: api/salesorders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SalesOrderDto>>> GetAll()
        {
            var orders = await _context.SalesOrders
                .Include(o => o.Customer)
                .Include(o => o.SalesOrderItems).ThenInclude(i => i.Vehicle)
                .ProjectToType<SalesOrderDto>()
                .ToListAsync();

            return Ok(orders);
        }

        // GET: api/salesorders/{id}
        [HttpGet("{id:long}")]
        public async Task<ActionResult<SalesOrderDto>> GetById(long id)
        {
            var order = await _context.SalesOrders
                .Include(o => o.Customer)
                .Include(o => o.SalesOrderItems).ThenInclude(i => i.Vehicle)
                .FirstOrDefaultAsync(o => o.SoId == id);

            if (order == null)
                return NotFound();

            return Ok(order.Adapt<SalesOrderDto>());
        }

        // POST: api/salesorders
        [HttpPost]
        public async Task<ActionResult<SalesOrderDto>> Create([FromBody] SalesOrderDto dto)
        {
            var order = dto.Adapt<Infrastructure.Persistence.Entities.SalesOrder>();
            order.OrderDate = DateOnly.FromDateTime(DateTime.Now);
            order.Status = "DRAFT";
            order.Subtotal = dto.Items.Sum(i => i.SellPrice);
            order.Discount = dto.Items.Sum(i => i.Discount);
            order.Tax = dto.Items.Sum(i => i.Tax);
            order.GrandTotal = order.Subtotal - order.Discount + order.Tax;

            _context.SalesOrders.Add(order);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = order.SoId }, order.Adapt<SalesOrderDto>());
        }

        // PUT: api/salesorders/{id}
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] SalesOrderDto dto)
        {
            var order = await _context.SalesOrders
                .Include(o => o.SalesOrderItems)
                .FirstOrDefaultAsync(o => o.SoId == id);

            if (order == null)
                return NotFound();

            dto.Adapt(order);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/salesorders/{id}
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var order = await _context.SalesOrders.FindAsync(id);
            if (order == null)
                return NotFound();

            _context.SalesOrders.Remove(order);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
