using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShowroomCar.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Authorization;

namespace ShowroomCar.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "RequireEmployee")]
    public class WaitlistsController : ControllerBase
    {
        private readonly ShowroomDbContext _db;
        public WaitlistsController(ShowroomDbContext db) => _db = db;

        public class WaitlistCreateRequest { public string Name { get; set; } = null!; }
        public class EntryCreateRequest
        {
            public int WaitlistId { get; set; }
            public long CustomerId { get; set; }
            public int ModelId { get; set; }
            public string? PreferredColor { get; set; }
            public DateOnly? RequestedDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> List()
        {
            var data = await _db.Waitlists.AsNoTracking().ToListAsync();
            return Ok(data);
        }

        [HttpPost]
        public async Task<ActionResult<object>> Create(WaitlistCreateRequest req)
        {
            var wl = new Waitlist { Name = req.Name };
            _db.Waitlists.Add(wl);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = wl.WaitlistId }, new { waitlistId = wl.WaitlistId });
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<object>> Get(int id)
        {
            var wl = await _db.Waitlists.AsNoTracking().FirstOrDefaultAsync(x => x.WaitlistId == id);
            if (wl == null) return NotFound();
            var entries = await _db.WaitlistEntries.AsNoTracking()
                .Where(e => e.WaitlistId == id)
                .OrderByDescending(e => e.RequestedDate)
                .ToListAsync();
            return Ok(new { wl.WaitlistId, wl.Name, Entries = entries });
        }

        [HttpPost("entries")]
        public async Task<ActionResult<object>> AddEntry(EntryCreateRequest req)
        {
            if (!await _db.Waitlists.AnyAsync(w => w.WaitlistId == req.WaitlistId))
                return NotFound("Waitlist not found.");
            if (!await _db.Customers.AnyAsync(c => c.CustomerId == req.CustomerId))
                return NotFound("Customer not found.");
            if (!await _db.VehicleModels.AnyAsync(m => m.ModelId == req.ModelId))
                return NotFound("Model not found.");

            var e = new WaitlistEntry
            {
                WaitlistId = req.WaitlistId,
                CustomerId = req.CustomerId,
                ModelId = req.ModelId,
                PreferredColor = req.PreferredColor,
                RequestedDate = req.RequestedDate,
                Status = "WAITING"
            };
            _db.WaitlistEntries.Add(e);
            await _db.SaveChangesAsync();
            return Ok(new { entryId = e.EntryId });
        }

        public class EntryUpdateStatusRequest { public string Status { get; set; } = null!; }

        [HttpPost("entries/{entryId:long}/status")]
        public async Task<ActionResult> UpdateEntryStatus(long entryId, EntryUpdateStatusRequest req)
        {
            var e = await _db.WaitlistEntries.FirstOrDefaultAsync(x => x.EntryId == entryId);
            if (e == null) return NotFound();
            e.Status = req.Status; // ví dụ: WAITING / CONTACTED / CONVERTED / CANCELLED
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
