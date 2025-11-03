using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShowroomCar.Domain.Constants;
using ShowroomCar.Infrastructure.Persistence.Entities;
using ShowroomCar.Application.Dtos; // ✅ dùng DTO riêng

namespace ShowroomCar.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "RequireEmployee")]
    public class ServiceOrdersController : ControllerBase
    {
        private readonly ShowroomDbContext _db;
        private readonly IHttpContextAccessor _http;

        public ServiceOrdersController(ShowroomDbContext db, IHttpContextAccessor http)
        { _db = db; _http = http; }

        private static string NewNo(string pfx) => $"{pfx}-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        private long? CurrentUserId()
        {
            var u = _http.HttpContext?.User;
            var sub = u?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? u?.FindFirst("sub")?.Value;
            return long.TryParse(sub, out var id) ? id : (long?)null;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ServiceOrderDto>>> List(
            [FromQuery] long? vehicleId, [FromQuery] string? status,
            [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate)
        {
            var q = _db.ServiceOrders.AsNoTracking().AsQueryable();
            if (vehicleId.HasValue) q = q.Where(s => s.VehicleId == vehicleId.Value);
            if (!string.IsNullOrWhiteSpace(status)) q = q.Where(s => s.Status == status);
            if (fromDate.HasValue) q = q.Where(s => s.ScheduledDate >= fromDate.Value);
            if (toDate.HasValue) q = q.Where(s => s.ScheduledDate <= toDate.Value);

            var data = await q
                .OrderByDescending(s => s.SvcId)
                .Select(s => new ServiceOrderDto
                {
                    SvcId = s.SvcId,
                    SvcNo = s.SvcNo,
                    VehicleId = s.VehicleId,
                    VehicleVin = _db.Vehicles.Where(v => v.VehicleId == s.VehicleId).Select(v => v.Vin).FirstOrDefault() ?? "",
                    ScheduledDate = s.ScheduledDate,
                    Status = s.Status,
                    Notes = s.Notes
                })
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<ServiceOrderDto>> Get(long id)
        {
            var s = await _db.ServiceOrders.AsNoTracking().FirstOrDefaultAsync(x => x.SvcId == id);
            if (s == null) return NotFound();

            var vin = await _db.Vehicles.AsNoTracking()
                         .Where(v => v.VehicleId == s.VehicleId)
                         .Select(v => v.Vin).FirstOrDefaultAsync() ?? "";

            return Ok(new ServiceOrderDto
            {
                SvcId = s.SvcId,
                SvcNo = s.SvcNo,
                VehicleId = s.VehicleId,
                VehicleVin = vin,
                ScheduledDate = s.ScheduledDate,
                Status = s.Status,
                Notes = s.Notes
            });
        }

        [HttpPost]
        public async Task<ActionResult<object>> Create([FromBody] ServiceOrderCreateRequest req)
        {
            var vehicle = await _db.Vehicles.FindAsync(req.VehicleId);
            if (vehicle == null) return NotFound($"Vehicle #{req.VehicleId} not found");

            var s = new ServiceOrder
            {
                SvcNo = NewNo("SVC"),
                VehicleId = req.VehicleId,
                ScheduledDate = req.ScheduledDate,
                Status = ServiceOrderStatus.Planned,
                Notes = req.Notes,
                CreatedBy = CurrentUserId(),
                CreatedAt = DateTime.UtcNow
            };

            _db.ServiceOrders.Add(s);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = s.SvcId }, new { s.SvcId, s.SvcNo });
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] ServiceOrderUpdateRequest req)
        {
            var s = await _db.ServiceOrders.FirstOrDefaultAsync(x => x.SvcId == id);
            if (s == null) return NotFound();

            if (s.Status == ServiceOrderStatus.Done || s.Status == ServiceOrderStatus.Cancelled)
                return Conflict($"ServiceOrder #{id} already {s.Status}, cannot modify.");

            s.ScheduledDate = req.ScheduledDate;
            s.Notes = req.Notes;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{id:long}/start")]
        public async Task<IActionResult> Start(long id)
        {
            var s = await _db.ServiceOrders.FirstOrDefaultAsync(x => x.SvcId == id);
            if (s == null) return NotFound();
            if (s.Status != ServiceOrderStatus.Planned)
                return Conflict($"Only {ServiceOrderStatus.Planned} can be started.");

            s.Status = ServiceOrderStatus.InProgress;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{id:long}/complete")]
        public async Task<IActionResult> Complete(long id)
        {
            var s = await _db.ServiceOrders.FirstOrDefaultAsync(x => x.SvcId == id);
            if (s == null) return NotFound();
            if (s.Status != ServiceOrderStatus.InProgress)
                return Conflict($"Only {ServiceOrderStatus.InProgress} can be completed.");

            s.Status = ServiceOrderStatus.Done;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{id:long}/cancel")]
        public async Task<IActionResult> Cancel(long id)
        {
            var s = await _db.ServiceOrders.FirstOrDefaultAsync(x => x.SvcId == id);
            if (s == null) return NotFound();
            if (s.Status == ServiceOrderStatus.Done)
                return Conflict("Completed orders cannot be cancelled.");

            s.Status = ServiceOrderStatus.Cancelled;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [Authorize(Policy = "RequireAdmin")]
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var s = await _db.ServiceOrders.FindAsync(id);
            if (s == null) return NotFound();
            if (s.Status == ServiceOrderStatus.InProgress || s.Status == ServiceOrderStatus.Done)
                return Conflict("Cannot delete a service order in-progress or completed.");

            _db.ServiceOrders.Remove(s);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
