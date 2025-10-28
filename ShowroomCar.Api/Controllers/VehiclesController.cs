using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShowroomCar.Infrastructure.Persistence.Entities;
using ShowroomCar.Application.Dtos;
using Mapster;

namespace ShowroomCar.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehiclesController : ControllerBase
    {
        private readonly ShowroomDbContext _context;

        public VehiclesController(ShowroomDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<VehicleDto>>> GetAll()
        {
            var vehicles = await _context.Vehicles
                .Include(v => v.Model)
                .ProjectToType<VehicleDto>()   // Map entity → DTO
                .ToListAsync();

            return Ok(vehicles);
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<VehicleDto>> GetById(long id)
        {
            var vehicle = await _context.Vehicles
                .Include(v => v.Model)
                .FirstOrDefaultAsync(v => v.VehicleId == id);

            if (vehicle == null)
                return NotFound();

            return Ok(vehicle.Adapt<VehicleDto>());  // Map từng item → DTO
        }

        [HttpPost]
        public async Task<ActionResult> Create(VehicleDto dto)
        {
            var entity = dto.Adapt<Infrastructure.Persistence.Entities.Vehicle>();  // Map DTO → entity
            _context.Vehicles.Add(entity);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = entity.VehicleId }, entity.Adapt<VehicleDto>());
        }
    }
}
