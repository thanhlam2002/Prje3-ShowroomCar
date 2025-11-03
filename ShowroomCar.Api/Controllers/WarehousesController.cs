using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShowroomCar.Infrastructure.Persistence.Entities;

namespace ShowroomCar.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "RequireAdmin")]
    public class WarehousesController : ControllerBase
    {
        private readonly ShowroomDbContext _db;
        public WarehousesController(ShowroomDbContext db) => _db = db;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Warehouse>>> List()
            => Ok(await _db.Warehouses.AsNoTracking().OrderBy(x => x.Code).ToListAsync());

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Warehouse>> Get(int id)
        {
            var w = await _db.Warehouses.FindAsync(id);
            return w == null ? NotFound() : Ok(w);
        }

        [HttpPost]
        public async Task<ActionResult<object>> Create(Warehouse w)
        {
            if (string.IsNullOrWhiteSpace(w.Code) || string.IsNullOrWhiteSpace(w.Name))
                return BadRequest("Code/Name required.");
            _db.Warehouses.Add(w);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = w.WarehouseId }, new { w.WarehouseId });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, Warehouse req)
        {
            var w = await _db.Warehouses.FirstOrDefaultAsync(x => x.WarehouseId == id);
            if (w == null) return NotFound();
            w.Code = req.Code; w.Name = req.Name; w.Address = req.Address;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var inUse = await _db.Vehicles.AnyAsync(v => v.CurrentWarehouseId == id)
                        || await _db.InventoryMoves.AnyAsync(m => m.FromWarehouseId == id || m.ToWarehouseId == id);
            if (inUse) return Conflict("Warehouse is referenced by vehicles/inventory moves.");

            var w = await _db.Warehouses.FindAsync(id);
            if (w == null) return NotFound();
            _db.Warehouses.Remove(w);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
