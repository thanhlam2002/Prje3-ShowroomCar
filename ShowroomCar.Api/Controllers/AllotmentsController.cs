// ShowroomCar.Api/Controllers/AllotmentsController.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShowroomCar.Infrastructure.Persistence.Entities;

namespace ShowroomCar.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "RequireEmployee")] // Sales/CS có quyền giữ/nhả xe
    public class AllotmentsController : ControllerBase
    {
        private readonly ShowroomDbContext _db;
        public AllotmentsController(ShowroomDbContext db) => _db = db;

        // Trạng thái (khớp SQL sample)
        private const string VehInStock   = "IN_STOCK";
        private const string VehAllocated = "ALLOCATED";
        private const string VehSold      = "SOLD";

        private const string AllotReserved = "RESERVED";
        private const string AllotReleased = "RELEASED";

        private static bool Is(string? s, string expected) =>
            string.Equals(s, expected, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Đặt giữ xe (RESERVE) cho SalesOrder.
        /// Điều kiện: vehicle phải đang IN_STOCK.
        /// Bảng allotments ràng buộc vehicle_id UNIQUE → 1-1.
        /// </summary>
        [HttpPost("reserve")]
        public async Task<ActionResult<AllotmentDto>> Reserve([FromBody] AllotmentReserveRequest req)
        {
            var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == req.VehicleId);
            if (vehicle == null) return NotFound($"Vehicle #{req.VehicleId} not found");
            if (!Is(vehicle.Status, VehInStock))
                return Conflict($"Vehicle #{vehicle.Vin} not IN_STOCK (status: {vehicle.Status}).");

            var so = await _db.SalesOrders.FirstOrDefaultAsync(x => x.SoId == req.SoId);
            if (so == null) return NotFound($"SalesOrder #{req.SoId} not found");

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var existing = await _db.Allotments.FirstOrDefaultAsync(a => a.VehicleId == req.VehicleId);

                if (existing != null && Is(existing.Status, AllotReserved))
                    return Conflict("This vehicle already has an active allotment (RESERVED).");

                if (existing == null)
                {
                    var allot = new Allotment
                    {
                        SoId       = req.SoId,
                        VehicleId  = req.VehicleId,
                        ReservedAt = DateTime.UtcNow,
                        Status     = AllotReserved
                    };
                    await _db.Allotments.AddAsync(allot);
                }
                else
                {
                    existing.SoId       = req.SoId;
                    existing.ReservedAt = DateTime.UtcNow;
                    existing.Status     = AllotReserved;
                    _db.Allotments.Update(existing);
                }

                vehicle.Status    = VehAllocated;
                vehicle.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                var allotRow = await _db.Allotments.AsNoTracking().FirstAsync(a => a.VehicleId == req.VehicleId);
                var dto = new AllotmentDto
                {
                    AllotId    = allotRow.AllotId,
                    SoId       = allotRow.SoId,
                    VehicleId  = allotRow.VehicleId,
                    ReservedAt = allotRow.ReservedAt,
                    Status     = allotRow.Status
                };
                return Ok(dto);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Hủy giữ (RELEASE). Trả xe về IN_STOCK nếu chưa SOLD.
        /// </summary>
        [HttpPost("{allotId:long}/release")]
        public async Task<ActionResult> Release(long allotId)
        {
            var allot = await _db.Allotments.FirstOrDefaultAsync(a => a.AllotId == allotId);
            if (allot == null) return NotFound();

            var veh = await _db.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == allot.VehicleId);
            if (veh == null) return NotFound($"Vehicle #{allot.VehicleId} not found");

            if (Is(allot.Status, AllotReleased)) return NoContent();

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                allot.Status = AllotReleased;

                if (!Is(veh.Status, VehSold)) // nếu chưa bán thì trả về kho
                {
                    veh.Status    = VehInStock;
                    veh.UpdatedAt = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
                return NoContent();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Danh sách allotments (lọc theo soId/vehicleId/status).
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AllotmentDto>>> List(
            [FromQuery] long? soId, [FromQuery] long? vehicleId, [FromQuery] string? status)
        {
            var q = _db.Allotments.AsNoTracking().AsQueryable();
            if (soId.HasValue)      q = q.Where(x => x.SoId == soId.Value);
            if (vehicleId.HasValue) q = q.Where(x => x.VehicleId == vehicleId.Value);
            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(x => x.Status == status);

            var data = await q
                .OrderByDescending(x => x.ReservedAt)
                .Select(a => new AllotmentDto
                {
                    AllotId    = a.AllotId,
                    SoId       = a.SoId,
                    VehicleId  = a.VehicleId,
                    ReservedAt = a.ReservedAt,
                    Status     = a.Status
                })
                .ToListAsync();

            return Ok(data);
        }
    }

    // ====== DTO nội bộ để file tự chạy (nếu bạn đã có DTO riêng, có thể xoá 2 class dưới) ======
    public class AllotmentReserveRequest
    {
        public long SoId { get; set; }
        public long VehicleId { get; set; }
    }

    public class AllotmentDto
    {
        public long AllotId { get; set; }
        public long SoId { get; set; }
        public long VehicleId { get; set; }
        public DateTime? ReservedAt { get; set; }
        public string Status { get; set; } = null!;
    }
}
