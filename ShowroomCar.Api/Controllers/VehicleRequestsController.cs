using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShowroomCar.Application.Dtos;
using ShowroomCar.Infrastructure.Persistence.Entities;

namespace ShowroomCar.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehicleRequestsController : ControllerBase
    {
        private readonly ShowroomDbContext _db;

        private const string ROLE_ADMIN = "ADMIN";
        private const string ROLE_EMPLOYEE = "EMPLOYEE";

        public VehicleRequestsController(ShowroomDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// CUSTOMER gửi contact đặt xe từ website
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CreateVehicleRequest([FromBody] VehicleRequestCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            long customerId;

            // Nếu Web KHÔNG truyền customerId → tự tạo customer
            if (dto.CustomerId == null || dto.CustomerId <= 0)
            {
                var newCustomer = new Customer
                {
                    FullName = dto.FullName,
                    Phone = dto.Phone,
                    Email = dto.Email,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Customers.Add(newCustomer);
                await _db.SaveChangesAsync();

                customerId = newCustomer.CustomerId;
            }
            else
            {
                customerId = dto.CustomerId.Value;
            }

            // Validate vehicle
            var vehicle = await _db.Vehicles
                .FirstOrDefaultAsync(v => v.VehicleId == dto.VehicleId);

            if (vehicle == null)
                return BadRequest("VehicleId không tồn tại.");

            var req = new VehicleRequest
            {
                CustomerId = customerId,
                FullName = dto.FullName,
                Phone = dto.Phone,
                Email = dto.Email,
                Content = dto.Content,
                VehicleId = dto.VehicleId,
                ModelId = vehicle.ModelId,
                PreferredColor = dto.PreferredColor,
                Source = dto.Source ?? "WEB",
                Status = "NEW",
                CreatedAt = DateTime.UtcNow
            };

            _db.VehicleRequests.Add(req);
            await _db.SaveChangesAsync();

            return Ok(new { requestId = req.RequestId });
        }





        /// <summary>
        /// ADMIN / EMPLOYEE xem danh sách request (lọc theo status)
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "RequireEmployee")]
        public async Task<ActionResult> GetRequests([FromQuery] string status = null)
        {
            var query = _db.VehicleRequests
                .Include(r => r.Model)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(r => r.Status == status);
            }

            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new VehicleRequestListDto
                {
                    RequestId = r.RequestId,
                    FullName = r.FullName,
                    Phone = r.Phone,
                    Email = r.Email,
                    PickupAppointment = r.PickupAppointment,
                    Status = r.Status,
                    PreferredColor = r.PreferredColor,
                    ModelName = r.Model.Name,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return Ok(items);
        }

        /// <summary>
        /// ADMIN / EMPLOYEE xem chi tiết 1 request
        /// </summary>
        [HttpGet("{id:long}")]
        [Authorize(Policy = "RequireEmployee")]
        public async Task<ActionResult> GetRequest(long id)
        {
            var r = await _db.VehicleRequests
                .Include(x => x.Vehicle)
                .ThenInclude(x => x.Model)
                .FirstOrDefaultAsync(x => x.RequestId == id);

            if (r == null)
                return NotFound();

            var dto = new VehicleRequestDetailDto
            {
                RequestId = r.RequestId,
                CustomerId = r.CustomerId,
                FullName = r.FullName,
                Phone = r.Phone,
                Email = r.Email,
                Content = r.Content,
                PickupAppointment = r.PickupAppointment,
                ModelId = r.ModelId,
                ModelName = r.Model?.Name,
                PreferredColor = r.PreferredColor,
                VehicleId = r.VehicleId,
                Source = r.Source,
                Status = r.Status,
                PoId = r.PoId,
                SoId = r.SoId,
                CreatedAt = r.CreatedAt,
                ProcessedBy = r.ProcessedBy,
                ProcessedAt = r.ProcessedAt,
                VehicleStatus = r.Vehicle?.Status,
                VehicleVin = r.Vehicle?.Vin,
                VehicleColor = r.Vehicle?.Color,
                VehicleModelName = r.Vehicle?.Model?.Name
            };

            return Ok(dto);
        }

        /// <summary>
        /// ADMIN / EMPLOYEE: gán 1 xe trong kho cho request (khi có xe)
        /// </summary>
        [HttpPut("{id:long}/assign-vehicle")]
        [Authorize(Policy = "RequireEmployee")]
        public async Task<IActionResult> AssignVehicle(long id, [FromQuery] long vehicleId)
        {
            var req = await _db.VehicleRequests
                .FirstOrDefaultAsync(r => r.RequestId == id);

            if (req == null)
                return NotFound("Request không tồn tại.");

            var vehicle = await _db.Vehicles
                .FirstOrDefaultAsync(v => v.VehicleId == vehicleId);

            if (vehicle == null)
                return NotFound("Vehicle không tồn tại.");

            // Có thể kiểm tra status xe: IN_STOCK,...
            if (vehicle.Status != "IN_STOCK")
            {
                return BadRequest("Xe không ở trạng thái IN_STOCK.");
            }

            // Gắn quan hệ
            vehicle.ReservedRequestId = req.RequestId;
            vehicle.ReservedForCustomerId = req.CustomerId;
            vehicle.Status = "RESERVED"; // bạn có thể tùy chỉnh

            req.VehicleId = vehicle.VehicleId;
            req.Status = "WAITING";

            await _db.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// ADMIN / EMPLOYEE: tạo PO cho request nếu không có xe trong kho
        /// (Tạo PO tối thiểu + tự sinh 1 PurchaseOrderItem theo Model khách chọn)
        /// </summary>
        [HttpPost("{id:long}/create-po")]
        [Authorize(Roles = ROLE_ADMIN + "," + ROLE_EMPLOYEE)]
        public async Task<IActionResult> CreatePurchaseOrderForRequest(long id)
        {
            var req = await _db.VehicleRequests
                .Include(r => r.Model)
                .FirstOrDefaultAsync(r => r.RequestId == id);

            if (req == null)
                return NotFound("Request không tồn tại.");

            if (req.PoId != null)
                return BadRequest("Request này đã có PO.");

            // Lấy Supplier từ model
            var supplierId = await _db.SupplierModels
                .Where(sm => sm.ModelId == req.ModelId)
                .Select(sm => sm.SupplierId)
                .FirstOrDefaultAsync();

            if (supplierId == 0)
                return BadRequest("Model này chưa có Supplier. Vui lòng cấu hình trước.");

            var po = new PurchaseOrder
            {
                PoNo = $"PO-{DateTime.UtcNow:yyyyMMddHHmmss}",
                SupplierId = supplierId,
                Status = "DRAFT",
                OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
                TotalAmount = 0,
                CreatedAt = DateTime.UtcNow,
                CustomerId = req.CustomerId,
                RequestId = req.RequestId
            };

            _db.PurchaseOrders.Add(po);
            await _db.SaveChangesAsync();

            // Tạo PO Item
            var item = new PurchaseOrderItem
            {
                PoId = po.PoId,
                ModelId = req.ModelId,
                Qty = 1,
                UnitPrice = 0,
                LineTotal = 0
            };

            _db.PurchaseOrderItems.Add(item);

            // Cập nhật request
            req.PoId = po.PoId;
            req.Status = "PO_CREATED";
            req.ProcessedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();


            return Ok(new { poId = po.PoId, poNo = po.PoNo });
        }


        /// <summary>
        /// ADMIN / EMPLOYEE: hủy request
        /// </summary>
        [HttpPut("{id:long}/cancel")]
        [Authorize(Policy = "RequireEmployee")]
        public async Task<IActionResult> Cancel(long id)
        {
            var req = await _db.VehicleRequests.FirstOrDefaultAsync(r => r.RequestId == id);
            if (req == null)
                return NotFound("Request không tồn tại.");

            req.Status = "CANCELED";
            await _db.SaveChangesAsync();

            return Ok();
        }
    }
}
