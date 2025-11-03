using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShowroomCar.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Authorization;

namespace ShowroomCar.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "RequireEmployee")]
    public class VehicleRegistrationsController : ControllerBase
    {
        private readonly ShowroomDbContext _db;
        public VehicleRegistrationsController(ShowroomDbContext db) => _db = db;

        public class RegistrationUpsertRequest
        {
            public long VehicleId { get; set; }
            public string? RegNo { get; set; }
            public DateOnly? RegDate { get; set; }
            public string? OwnerName { get; set; }
            public string? Address { get; set; }
            public string? FieldsJson { get; set; } // JSON string (optional)
        }

        [HttpGet("{vehicleId:long}")]
        public async Task<ActionResult<object>> Get(long vehicleId)
        {
            var reg = await _db.VehicleRegistrations.AsNoTracking()
                .FirstOrDefaultAsync(r => r.VehicleId == vehicleId);
            if (reg == null) return NotFound();
            return Ok(reg);
        }

        [HttpPost]
        public async Task<ActionResult<object>> Upsert(RegistrationUpsertRequest req)
        {
            var v = await _db.Vehicles.FindAsync(req.VehicleId);
            if (v == null) return NotFound("Vehicle not found.");

            var reg = await _db.VehicleRegistrations.FirstOrDefaultAsync(r => r.VehicleId == req.VehicleId);
            if (reg == null)
            {
                reg = new VehicleRegistration
                {
                    VehicleId = req.VehicleId,
                    RegNo = req.RegNo,
                    RegDate = req.RegDate,
                    OwnerName = req.OwnerName,
                    Address = req.Address,
                    Fields = req.FieldsJson
                };
                _db.VehicleRegistrations.Add(reg);
            }
            else
            {
                reg.RegNo = req.RegNo;
                reg.RegDate = req.RegDate;
                reg.OwnerName = req.OwnerName;
                reg.Address = req.Address;
                reg.Fields = req.FieldsJson;
                _db.VehicleRegistrations.Update(reg);
            }
            await _db.SaveChangesAsync();
            return Ok(new { vehicleId = req.VehicleId, regNo = req.RegNo });
        }
    }
}
