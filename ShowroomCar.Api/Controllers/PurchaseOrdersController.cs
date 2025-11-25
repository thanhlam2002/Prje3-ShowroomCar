using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShowroomCar.Application.Dtos;
using ShowroomCar.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Authorization;
using ShowroomCar.Api.Services;

namespace ShowroomCar.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "RequireAdmin")]
    public class PurchaseOrdersController : ControllerBase
    {
        private readonly ShowroomDbContext _db;
        public PurchaseOrdersController(ShowroomDbContext db) => _db = db;

        private static string NewNo(string pfx) => $"{pfx}-{DateTime.UtcNow:yyyyMMddHHmmssfff}";

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PurchaseOrderDto>>> List()
        {
            var data = await _db.PurchaseOrders
                .AsNoTracking()
                .Include(p => p.Supplier)
                .Include(p => p.PurchaseOrderItems)
                .OrderByDescending(p => p.PoId)
                .ToListAsync();

            var dtos = data.Select(p => new PurchaseOrderDto
            {
                PoId = p.PoId,
                PoNo = p.PoNo,
                SupplierId = p.SupplierId,
                SupplierName = p.Supplier.Name,
                OrderDate = p.OrderDate,
                Status = p.Status,
                TotalAmount = p.TotalAmount,
                Items = p.PurchaseOrderItems.Select(i => new PurchaseOrderItemDto
                {
                    PoItemId = i.PoItemId,
                    PoId = p.PoId,
                    ModelId = i.ModelId,
                    Qty = i.Qty,
                    UnitPrice = i.UnitPrice,
                    LineTotal = i.LineTotal
                }).ToList()
            });

            return Ok(dtos);
        }

        [HttpPost]
        public async Task<ActionResult<PurchaseOrderDto>> Create(PurchaseOrderCreateRequest req)
        {
            if (req.Items == null || req.Items.Count == 0)
                return BadRequest("PO must have at least one item.");

            var sup = await _db.Suppliers.FindAsync(req.SupplierId);
            if (sup == null) return NotFound("Supplier not found.");

            var modelIds = req.Items.Select(x => x.ModelId).Distinct().ToList();
            var existing = await _db.VehicleModels
                .Where(m => modelIds.Contains(m.ModelId))
                .Select(m => m.ModelId)
                .ToListAsync();

            if (existing.Count != modelIds.Count)
                return BadRequest("Some model IDs do not exist.");

            var po = new PurchaseOrder
            {
                PoNo = NewNo("PO"),
                SupplierId = req.SupplierId,
                Status = "PENDING",
                OrderDate = req.OrderDate,
                TotalAmount = 0,
                CreatedBy = null,
                CreatedAt = DateTime.UtcNow
            };
            await _db.PurchaseOrders.AddAsync(po);
            await _db.SaveChangesAsync();

            decimal total = 0;
            var items = new List<PurchaseOrderItem>();
            foreach (var x in req.Items)
            {
                var lt = x.UnitPrice * x.Qty;
                items.Add(new PurchaseOrderItem
                {
                    PoId = po.PoId,
                    ModelId = x.ModelId,
                    Qty = x.Qty,
                    UnitPrice = x.UnitPrice,
                    LineTotal = lt
                });
                total += lt;
            }

            await _db.PurchaseOrderItems.AddRangeAsync(items);
            po.TotalAmount = total;
            _db.PurchaseOrders.Update(po);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = po.PoId }, new { poId = po.PoId, po.PoNo });
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<PurchaseOrderDto>> Get(long id)
        {
            var p = await _db.PurchaseOrders
                .AsNoTracking()
                .Include(x => x.Supplier)
                .Include(x => x.PurchaseOrderItems)
                .FirstOrDefaultAsync(x => x.PoId == id);

            if (p == null) return NotFound();

            return Ok(new PurchaseOrderDto
            {
                PoId = p.PoId,
                PoNo = p.PoNo,
                SupplierId = p.SupplierId,
                SupplierName = p.Supplier.Name,
                OrderDate = p.OrderDate,
                Status = p.Status,
                TotalAmount = p.TotalAmount,
                Items = p.PurchaseOrderItems.Select(i => new PurchaseOrderItemDto
                {
                    PoItemId = i.PoItemId,
                    PoId = i.PoId,
                    ModelId = i.ModelId,
                    Qty = i.Qty,
                    UnitPrice = i.UnitPrice,
                    LineTotal = i.LineTotal
                }).ToList()
            });
        }

        // ✅ NEW: Approve PO → auto create GR + ServiceOrders
        // [HttpPost("{id:long}/approve")]
        // public async Task<IActionResult> Approve(long id)
        // {
        //     var po = await _db.PurchaseOrders
        //         .Include(p => p.PurchaseOrderItems)
        //         .FirstOrDefaultAsync(p => p.PoId == id);

        //     if (po == null) return NotFound();
        //     if (po.Status == "APPROVED") return Conflict("Already approved.");

        //     await using var tx = await _db.Database.BeginTransactionAsync();
        //     try
        //     {
        //         po.Status = "APPROVED";
        //         _db.PurchaseOrders.Update(po);

        //         // 1️⃣ Tạo phiếu nhập (GR)
        //         var gr = new GoodsReceipt
        //         {
        //             GrNo = $"GR-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
        //             PoId = po.PoId,
        //             WarehouseId = 1, // TODO: replace with config
        //             ReceiptDate = DateOnly.FromDateTime(DateTime.UtcNow),
        //             CreatedAt = DateTime.UtcNow
        //         };
        //         _db.GoodsReceipts.Add(gr);
        //         await _db.SaveChangesAsync();

        //         var createdVehicles = new List<Vehicle>();
        //         var now = DateTime.UtcNow;

        //         // 2️⃣ Sinh xe
        //         foreach (var item in po.PurchaseOrderItems)
        //         {
        //             for (int i = 0; i < item.Qty; i++)
        //             {
        //                 var suffix = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
        //                 var vin = $"VIN-{item.ModelId:D2}-{DateTime.UtcNow:HHmmss}-{suffix}";
        //                 var eng = $"ENG-{item.ModelId:D2}-{DateTime.UtcNow:HHmmss}-{suffix}";

        //                 var veh = new Vehicle
        //                 {
        //                     ModelId = item.ModelId,
        //                     Vin = vin,
        //                     EngineNo = eng,
        //                     Color = "White",
        //                     Year = DateTime.UtcNow.Year,
        //                     Status = "INSPECTION_PENDING",
        //                     AcquiredAt = now,
        //                     UpdatedAt = now
        //                 };
        //                 _db.Vehicles.Add(veh);
        //                 createdVehicles.Add(veh);
        //             }
        //         }

        //         await _db.SaveChangesAsync();

        //         // 3️⃣ Ghi chi tiết phiếu nhập (GR Items)
        //         var grItems = createdVehicles.Select(v => new GoodsReceiptItem
        //         {
        //             GrId = gr.GrId,
        //             VehicleId = v.VehicleId,
        //             LandedCost = po.PurchaseOrderItems
        //                 .FirstOrDefault(x => x.ModelId == v.ModelId)?.UnitPrice ?? 0
        //         });
        //         await _db.GoodsReceiptItems.AddRangeAsync(grItems);

        //         // 4️⃣ Tạo ServiceOrder theo model_id
        //         var modelGroups = createdVehicles.GroupBy(v => v.ModelId);
        //         foreach (var grp in modelGroups)
        //         {
        //             var svc = new ServiceOrder
        //             {
        //                 SvcNo = $"SVC-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{grp.Key}",
        //                 PoId = po.PoId,
        //                 GrId = gr.GrId,
        //                 ModelId = grp.Key,
        //                 QuantityExpected = grp.Count(),
        //                 ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow),
        //                 Status = "PLANNED",
        //                 Notes = $"Kiểm định lô xe model_id={grp.Key} ({grp.Count()} xe)",
        //                 CreatedAt = now
        //             };
        //             _db.ServiceOrders.Add(svc);
        //         }

        //         await _db.SaveChangesAsync();
        //         await tx.CommitAsync();

        //         return Ok(new { message = "PO approved, GR + Service Orders created", gr.GrId });
        //     }
        //     catch (Exception ex)
        //     {
        //         await tx.RollbackAsync();
        //         return StatusCode(500, $"Transaction failed: {ex.Message}");
        //     }
        // }

        // ✅ Gửi PO cho nhà cung cấp (send mail + đổi trạng thái)
        [HttpPost("{id:long}/send")]
        public async Task<IActionResult> Send(
    long id,
    [FromServices] MailService mailer,
    [FromServices] PoTokenService tokenSvc,
    [FromServices] IConfiguration config)
        {
            var po = await _db.PurchaseOrders
                .Include(p => p.Supplier)
                .Include(p => p.PurchaseOrderItems)
                .FirstOrDefaultAsync(p => p.PoId == id);

            if (po == null) return NotFound();

            // Cập nhật trạng thái khi gửi
            po.Status = "RECEIVING";
            await _db.SaveChangesAsync();

            // === TẠO TOKEN XÁC NHẬN ===
            var token = tokenSvc.Generate(po.PoId);

            var baseUrl = config["App:BaseUrl"]; // thêm mục này vào appsettings.json
            var confirmUrl = $"{baseUrl}/api/purchaseorders/{po.PoId}/confirm?token={token}";

            // === EMAIL NỘI DUNG ===
            var body = $@"
                <p>Kính gửi <b>{po.Supplier.Name}</b>,</p>
                <p>Đơn đặt hàng <b>{po.PoNo}</b> đã được gửi từ hệ thống ShowroomCar.</p>

                <p>Vui lòng xác nhận đơn hàng tại liên kết dưới đây:</p>
                <p>
                <a href=""{confirmUrl}"" 
                    style=""background:#4CAF50;color:white;padding:10px 18px;text-decoration:none;border-radius:4px;"">
                    Xác nhận Đơn hàng
                </a>
                </p>

                <p>Nếu không bấm được nút, copy link sau:</p>
                <p>{confirmUrl}</p>

                <hr/>
                <p>Email tự động từ hệ thống ShowroomCar.</p>";

            await mailer.SendPurchaseOrderAsync(
                po.Supplier.Email,
                $"[ShowroomCar] Xác nhận đơn đặt hàng {po.PoNo}",
                body
            );

            return Ok(new { message = "PO sent successfully", po.PoNo, po.Status });
        }
        [HttpGet("{id:long}/confirm")]
        [AllowAnonymous]  // Supplier không phải đăng nhập
        public async Task<IActionResult> Confirm(long id, string token, [FromServices] PoTokenService tokenSvc)
        {
            if (!tokenSvc.Validate(token, out var tokenPoId) || tokenPoId != id)
                return BadRequest("Token không hợp lệ hoặc đã hết hạn.");

            var po = await _db.PurchaseOrders.FirstOrDefaultAsync(p => p.PoId == id);
            if (po == null) return NotFound();

            if (po.Status == "CONFIRMED")
                return Ok("PO đã xác nhận trước đó.");

            po.Status = "CONFIRMED";
            await _db.SaveChangesAsync();

            // Trả về trang HTML nhỏ (supplier thấy đẹp hơn JSON)
            var html = $@"
                    <html>
                    <body style='font-family:Arial;'>
                    <h2>Đơn hàng {po.PoNo} đã được xác nhận thành công!</h2>
                    <p>Cảm ơn quý đối tác đã xác nhận. Chúng tôi sẽ tiến hành các bước tiếp theo.</p>
                    <hr/>
                    <small>ShowroomCar System</small>
                    </body>
                    </html>";

            return Content(html, "text/html");
        }

        [HttpPost("{id:long}/receive")]
        public async Task<IActionResult> Receive(long id, [FromBody] PurchaseOrderReceiveRequest req)
        {
            var po = await _db.PurchaseOrders
                .Include(p => p.PurchaseOrderItems)
                .FirstOrDefaultAsync(p => p.PoId == id);

            if (po == null)
                return NotFound("PO không tồn tại.");

            if (po.Status != "CONFIRMED")
                return BadRequest("PO phải ở trạng thái CONFIRMED trước khi nhập kho.");

            var warehouse = await _db.Warehouses.FirstOrDefaultAsync(w => w.WarehouseId == req.WarehouseId);
            if (warehouse == null)
                return BadRequest("Warehouse không tồn tại.");

            var now = DateTime.UtcNow;
            var year = req.Year ?? now.Year;
            var defaultColor = string.IsNullOrWhiteSpace(req.DefaultColor) ? "White" : req.DefaultColor;

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // 1) Tạo GR
                var gr = new GoodsReceipt
                {
                    GrNo = $"GR-{now:yyyyMMddHHmmssfff}",
                    PoId = po.PoId,
                    WarehouseId = req.WarehouseId,
                    ReceiptDate = DateOnly.FromDateTime(now),
                    CreatedAt = now
                };
                _db.GoodsReceipts.Add(gr);
                await _db.SaveChangesAsync();

                // 2) Sinh Vehicle
                var createdVehicles = new List<Vehicle>();

                foreach (var item in po.PurchaseOrderItems)
                {
                    for (int i = 0; i < item.Qty; i++)
                    {
                        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpper();

                        var veh = new Vehicle
                        {
                            ModelId = item.ModelId,
                            Vin = $"VIN-{item.ModelId}-{suffix}",
                            EngineNo = $"ENG-{item.ModelId}-{suffix}",
                            Color = defaultColor,
                            Year = year,
                            Status = "UNDER_INSPECTION",
                            CurrentWarehouseId = req.WarehouseId,
                            AcquiredAt = now,
                            UpdatedAt = now
                        };

                        createdVehicles.Add(veh);
                        _db.Vehicles.Add(veh);
                    }
                }

                await _db.SaveChangesAsync();

                // 3) GR Items
                var grItems = createdVehicles.Select(v =>
                    new GoodsReceiptItem
                    {
                        GrId = gr.GrId,
                        VehicleId = v.VehicleId,
                        LandedCost = po.PurchaseOrderItems.First(x => x.ModelId == v.ModelId).UnitPrice
                    });

                _db.GoodsReceiptItems.AddRange(grItems);

                // 4) Auto-assign cho Request (nếu có)
                if (po.RequestId != null)
                {
                    var reqId = po.RequestId.Value;
                    var reqEnt = await _db.VehicleRequests.FirstOrDefaultAsync(r => r.RequestId == reqId);

                    if (reqEnt != null)
                    {
                        Vehicle picked = null;

                        if (!string.IsNullOrWhiteSpace(reqEnt.PreferredColor))
                        {
                            picked = createdVehicles.FirstOrDefault(v =>
                                v.ModelId == reqEnt.ModelId &&
                                v.Color.ToLower() == reqEnt.PreferredColor.ToLower());
                        }

                        if (picked == null)
                        {
                            picked = createdVehicles.FirstOrDefault(v => v.ModelId == reqEnt.ModelId);
                        }

                        if (picked != null)
                        {
                            picked.Status = "RESERVED";
                            picked.ReservedRequestId = reqEnt.RequestId;
                            picked.ReservedForCustomerId = reqEnt.CustomerId;

                            reqEnt.VehicleId = picked.VehicleId;
                            reqEnt.Status = "WAITING";
                            reqEnt.ProcessedAt = now;
                        }
                    }
                }

                // 5) PO -> CLOSED
                po.Status = "CLOSED";
                await _db.SaveChangesAsync();

                await tx.CommitAsync();

                return Ok(new
                {
                    message = "PO received, vehicles created and auto-assigned (if applicable)",
                    grId = gr.GrId,
                    vehiclesCreated = createdVehicles.Count
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPut("{id:long}/supplier")]
        public async Task<IActionResult> UpdateSupplier(long id, [FromBody] PurchaseOrderUpdateSupplierRequest req)
        {
            var po = await _db.PurchaseOrders
                .Include(p => p.Supplier)
                .Include(p => p.PurchaseOrderItems)
                .FirstOrDefaultAsync(p => p.PoId == id);

            if (po == null)
                return NotFound("PO không tồn tại.");

            // Chỉ được sửa supplier khi PO chưa gửi
            if (po.Status != "PENDING" && po.Status != "PO_CREATED")
                return BadRequest("Không thể đổi Supplier khi PO đã gửi hoặc đã xác nhận.");

            var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.SupplierId == req.SupplierId);
            if (supplier == null)
                return BadRequest("Supplier không tồn tại.");

            // Cập nhật
            po.SupplierId = req.SupplierId;
            await _db.SaveChangesAsync();

            // Trả về PO đầy đủ để FE hiển thị
            var dto = new PurchaseOrderDto
            {
                PoId = po.PoId,
                PoNo = po.PoNo,
                SupplierId = po.SupplierId,
                SupplierName = supplier.Name,
                OrderDate = po.OrderDate,
                Status = po.Status,
                TotalAmount = po.TotalAmount,
                Items = po.PurchaseOrderItems.Select(i => new PurchaseOrderItemDto
                {
                    PoItemId = i.PoItemId,
                    PoId = i.PoId,
                    ModelId = i.ModelId,
                    Qty = i.Qty,
                    UnitPrice = i.UnitPrice,
                    LineTotal = i.LineTotal
                }).ToList()
            };

            return Ok(dto);
        }

    }
}
