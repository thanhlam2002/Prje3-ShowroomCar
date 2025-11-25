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
using ShowroomCar.Application.Dtos;

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

        // ✅ Danh sách ServiceOrder
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

        // ✅ Chi tiết
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

        // ✅ Tạo ServiceOrder (kiểm tra xe)
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

        // ✅ Cập nhật thông tin
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

        // ✅ Bắt đầu kiểm tra
        [HttpPost("{id:long}/start")]
        public async Task<IActionResult> Start(long id)
        {
            var s = await _db.ServiceOrders.FirstOrDefaultAsync(x => x.SvcId == id);
            if (s == null) return NotFound();
            if (s.Status != ServiceOrderStatus.Planned)
                return Conflict($"Only {ServiceOrderStatus.Planned} can be started.");

            // 1️⃣ Cập nhật ServiceOrder
            s.Status = ServiceOrderStatus.InProgress;

            // 2️⃣ Cập nhật trạng thái Vehicle
            if (s.Vehicle != null)
            {
                s.Vehicle.Status = "INSPECTION_IN_PROGRESS";
                s.Vehicle.UpdatedAt = DateTime.UtcNow;
            }

            s.Status = ServiceOrderStatus.InProgress;
            await _db.SaveChangesAsync();
            return Ok(new { message = $"Service {s.SvcNo} started." });
        }

        // ✅ Hoàn tất kiểm tra — có kết quả PASSED / FAILED
        // ✅ Hoàn tất kiểm tra theo lô model_id
        [HttpPost("{id:long}/complete")]
        public async Task<IActionResult> Complete(long id, [FromBody] ServiceOrderCompleteRequest req)
        {
            var svc = await _db.ServiceOrders
                .Include(x => x.Vehicle)
                .FirstOrDefaultAsync(x => x.SvcId == id);
            if (svc == null) return NotFound();
            if (svc.Status != ServiceOrderStatus.InProgress)
                return Conflict($"Only {ServiceOrderStatus.InProgress} can be completed.");

            if ((req.PassedVehicles == null || req.PassedVehicles.Count == 0) &&
                (req.FailedVehicles == null || req.FailedVehicles.Count == 0))
                return BadRequest("At least one vehicle must be specified.");

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var now = DateTime.UtcNow;
                var userId = CurrentUserId();

                // ✅ Xe đạt kiểm định → nhập kho
                var passed = await _db.Vehicles
                    .Where(v => req.PassedVehicles != null && req.PassedVehicles.Contains(v.VehicleId))
                    .ToListAsync();

                foreach (var v in passed)
                {
                    v.Status = "IN_STOCK";
                    v.UpdatedAt = now;

                    // Lấy warehouseId từ Vehicle thay vì hardcode
                    var warehouseId = v.CurrentWarehouseId ?? 1; // Fallback nếu null

                    await _db.InventoryMoves.AddAsync(new InventoryMove
                    {
                        VehicleId = v.VehicleId,
                        ToWarehouseId = warehouseId,
                        FromWarehouseId = null,
                        Reason = "INSPECTION_APPROVED",
                        MovedAt = now,
                        MovedBy = userId
                    });
                }

                // ❌ Xe trượt kiểm định → tạo phiếu trả
                long? poIdForReturn = null;
                int? supplierIdForReturn = null;

                if (req.FailedVehicles?.Count > 0)
                {
                    // Lấy PoId và SupplierId từ Vehicle thông qua GoodsReceiptItem -> GoodsReceipt -> PurchaseOrder
                    var failedVehicleIds = req.FailedVehicles.ToList();
                    var grItems = await _db.GoodsReceiptItems
                        .Include(gri => gri.Gr)
                        .ThenInclude(gr => gr.Po)
                        .Where(gri => failedVehicleIds.Contains(gri.VehicleId))
                        .ToListAsync();

                    if (grItems.Any())
                    {
                        var firstGr = grItems.First().Gr;
                        poIdForReturn = firstGr.PoId;
                        if (firstGr.Po != null)
                        {
                            supplierIdForReturn = firstGr.Po.SupplierId;
                        }
                    }

                    // Nếu không tìm thấy qua GR, thử lấy từ ServiceOrder
                    if (!poIdForReturn.HasValue && svc.PoId.HasValue && svc.PoId.Value > 0)
                    {
                        poIdForReturn = svc.PoId.Value;
                        var po = await _db.PurchaseOrders.FindAsync(svc.PoId.Value);
                        if (po != null)
                        {
                            supplierIdForReturn = po.SupplierId;
                        }
                    }

                    // Fallback: nếu vẫn không có, không tạo GoodsReturn (hoặc log warning)
                    if (poIdForReturn.HasValue && supplierIdForReturn.HasValue)
                    {
                        var gr = new GoodsReturn
                        {
                            GrtNo = $"GRT-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                            PoId = poIdForReturn.Value,
                            SupplierId = supplierIdForReturn.Value,
                            ReturnDate = DateOnly.FromDateTime(now),
                            CreatedAt = now
                        };
                        await _db.GoodsReturns.AddAsync(gr);
                        await _db.SaveChangesAsync();

                        foreach (var vId in req.FailedVehicles)
                        {
                            await _db.GoodsReturnItems.AddAsync(new GoodsReturnItem
                            {
                                GrtId = gr.GrtId,
                                VehicleId = vId,
                                Reason = "Failed inspection"
                            });

                            var v = await _db.Vehicles.FindAsync(vId);
                            if (v != null)
                            {
                                v.Status = "RETURNED";
                                v.UpdatedAt = now;
                            }
                        }
                    }
                }

                svc.Status = ServiceOrderStatus.Done;
                svc.Notes += $"\nInspection completed: {passed.Count} approved, {req.FailedVehicles?.Count ?? 0} rejected.";
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                // ✅ Kiểm tra nếu tất cả xe thuộc PO đã kiểm định xong → đóng PO
                // Lấy PoId từ ServiceOrder hoặc từ Vehicle thông qua GR
                long? poIdToCheck = (svc.PoId.HasValue && svc.PoId.Value > 0) ? svc.PoId.Value : poIdForReturn;

                if (poIdToCheck.HasValue && poIdToCheck.Value > 0)
                {
                    var po = await _db.PurchaseOrders
                        .Include(p => p.PurchaseOrderItems)
                        .FirstOrDefaultAsync(p => p.PoId == poIdToCheck.Value);

                    if (po != null)
                    {
                        var totalOrdered = po.PurchaseOrderItems.Sum(i => i.Qty);

                        // ✅ Đếm số xe thuộc đúng PO đó thông qua GoodsReceipt -> GoodsReceiptItem -> Vehicle
                        // Chỉ đếm xe đã kiểm định xong (IN_STOCK hoặc RETURNED)
                        var doneCount = await _db.GoodsReceiptItems
                            .Include(gri => gri.Gr)
                            .Include(gri => gri.Vehicle)
                            .Where(gri => gri.Gr.PoId == po.PoId &&
                                         (gri.Vehicle.Status == "IN_STOCK" || gri.Vehicle.Status == "RETURNED"))
                            .CountAsync();

                        if (doneCount >= totalOrdered)
                        {
                            po.Status = "CLOSED";
                            _db.PurchaseOrders.Update(po);
                            await _db.SaveChangesAsync();
                        }
                    }
                }



                return Ok(new
                {
                    svc.SvcId,
                    svc.SvcNo,
                    svc.Status,
                    Passed = passed.Count,
                    Failed = req.FailedVehicles?.Count ?? 0
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, $"Transaction failed: {ex.Message}");
            }
        }


        // ✅ Huỷ service order
        [HttpPost("{id:long}/cancel")]
        public async Task<IActionResult> Cancel(long id)
        {
            var s = await _db.ServiceOrders.FirstOrDefaultAsync(x => x.SvcId == id);
            if (s == null) return NotFound();
            if (s.Status == ServiceOrderStatus.Done)
                return Conflict("Completed orders cannot be cancelled.");

            s.Status = ServiceOrderStatus.Cancelled;
            await _db.SaveChangesAsync();
            return Ok(new { message = $"Service {s.SvcNo} cancelled." });
        }

        // ✅ Xoá (admin-only)
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
