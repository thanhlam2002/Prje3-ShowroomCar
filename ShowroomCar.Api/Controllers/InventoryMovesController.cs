using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShowroomCar.Infrastructure.Persistence.Entities;
using System.Linq;

namespace ShowroomCar.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "RequireAdmin")]
    public class InventoryMovesController : ControllerBase
    {
        private readonly ShowroomDbContext _db;
        public InventoryMovesController(ShowroomDbContext db) => _db = db;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> List(
            [FromQuery] long? vehicleId,
            [FromQuery] int? fromWarehouseId,
            [FromQuery] int? toWarehouseId,
            [FromQuery] string? reason,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var q = _db.InventoryMoves.AsNoTracking().AsQueryable();
            if (vehicleId.HasValue)    q = q.Where(x => x.VehicleId == vehicleId.Value);
            if (fromWarehouseId.HasValue) q = q.Where(x => x.FromWarehouseId == fromWarehouseId.Value);
            if (toWarehouseId.HasValue)   q = q.Where(x => x.ToWarehouseId == toWarehouseId.Value);
            if (!string.IsNullOrWhiteSpace(reason)) q = q.Where(x => x.Reason == reason);
            if (from.HasValue) q = q.Where(x => x.MovedAt >= from.Value);
            if (to.HasValue)   q = q.Where(x => x.MovedAt <= to.Value);

            var data = await q
                .OrderByDescending(x => x.MovedAt)
                .Select(x => new {
                    x.MoveId, x.VehicleId, x.FromWarehouseId, x.ToWarehouseId,
                    x.Reason, x.MovedAt, x.MovedBy
                }).ToListAsync();

            return Ok(data);
        }
    }
}
