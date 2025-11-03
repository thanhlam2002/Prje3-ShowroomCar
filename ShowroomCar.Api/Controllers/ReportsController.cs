using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ShowroomCar.Infrastructure.Persistence.Entities;

namespace ShowroomCar.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "RequireEmployee")]
    public class ReportsController : ControllerBase
    {
        private readonly ShowroomDbContext _db;
        public ReportsController(ShowroomDbContext db) => _db = db;

        // ========= 1) STOCK SUMMARY =========
        // GET /api/reports/stock            -> tổng theo status
        // GET /api/reports/stock?by=model   -> tổng theo model + status
        [HttpGet("stock")]
        public async Task<ActionResult<object>> Stock([FromQuery] string? by)
        {
            if (string.Equals(by, "model", StringComparison.OrdinalIgnoreCase))
            {
                var rows = await _db.Vehicles
                    .GroupBy(v => new { v.ModelId, v.Status })
                    .Select(g => new { g.Key.ModelId, g.Key.Status, Count = g.Count() })
                    .ToListAsync();

                var modelIds = rows.Select(r => r.ModelId).Distinct().ToList();
                var names = await _db.VehicleModels
                    .Where(m => modelIds.Contains(m.ModelId))
                    .Select(m => new { m.ModelId, m.Name })
                    .ToDictionaryAsync(x => x.ModelId, x => x.Name);

                var result = rows.Select(r => new
                {
                    r.ModelId,
                    ModelName = names.TryGetValue(r.ModelId, out var n) ? n : "",
                    r.Status,
                    r.Count
                });

                return Ok(result);
            }
            else
            {
                var result = await _db.Vehicles
                    .GroupBy(v => v.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();

                return Ok(result);
            }
        }

        // ========= 2) ACTIVE ALLOTMENTS =========
        // GET /api/reports/allotments/active
        [HttpGet("allotments/active")]
        public async Task<ActionResult<IEnumerable<object>>> ActiveAllotments()
        {
            const string Reserved = "RESERVED";

            var data = await _db.Allotments
                .Where(a => a.Status == Reserved)
                .Join(_db.Vehicles, a => a.VehicleId, v => v.VehicleId,
                    (a, v) => new { a, v })
                .Join(_db.SalesOrders, av => av.a.SoId, s => s.SoId,
                    (av, s) => new { av.a, av.v, s })
                .Join(_db.Customers, avs => avs.s.CustomerId, c => c.CustomerId,
                    (avs, c) => new
                    {
                        avs.a.VehicleId,
                        avs.v.Vin,
                        avs.s.SoId,
                        avs.s.SoNo,
                        c.CustomerId,
                        CustomerName = c.FullName,
                        avs.a.ReservedAt
                    })
                .OrderByDescending(x => x.ReservedAt)
                .ToListAsync();

            return Ok(data);
        }

        // ========= 3) SALES (REVENUE) BY DAY =========
        // GET /api/reports/sales?from=2025-10-01&to=2025-11-05
        [HttpGet("sales")]
        public async Task<ActionResult<object>> Sales([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var start = from ?? today.AddDays(-30);
            var end   = to   ?? today;

            var q = _db.Invoices
                .AsNoTracking()
                .Where(i => i.InvoiceDate >= start && i.InvoiceDate <= end);

            var points = await q
                .GroupBy(i => i.InvoiceDate)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(x => x.GrandTotal) })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var total = points.Sum(p => p.Revenue);

            return Ok(new
            {
                From = start,
                To   = end,
                TotalRevenue = total,
                Series = points // [{ date, revenue }]
            });
        }

        // ========= 4) AR AGING (OPEN INVOICES) =========
        // GET /api/reports/ar-aging
        [HttpGet("ar-aging")]
        public async Task<ActionResult<object>> ArAging()
        {
            // Open = chưa thanh toán đủ
            var open = await _db.Invoices
                .AsNoTracking()
                .Where(i => i.Status != "PAID_FULL")
                .Select(i => new
                {
                    i.InvoiceId,
                    i.InvoiceNo,
                    i.InvoiceDate,
                    i.GrandTotal,
                    Allocated = _db.PaymentAllocations
                        .Where(a => a.InvoiceId == i.InvoiceId)
                        .Sum(a => (decimal?)a.AmountApplied) ?? 0m
                })
                .ToListAsync();

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            decimal b0_30 = 0, b31_60 = 0, b61_90 = 0, b90p = 0;

            foreach (var x in open)
            {
                var due = x.GrandTotal - x.Allocated;
                if (due <= 0) continue;

                var days = (today.ToDateTime(TimeOnly.MinValue) - x.InvoiceDate.ToDateTime(TimeOnly.MinValue)).Days;
                if (days <= 30)        b0_30 += due;
                else if (days <= 60)   b31_60 += due;
                else if (days <= 90)   b61_90 += due;
                else                   b90p   += due;
            }

            var total = b0_30 + b31_60 + b61_90 + b90p;

            return Ok(new
            {
                TotalOpen = total,
                Buckets = new[]
                {
                    new { Bucket = "0-30",  Amount = b0_30 },
                    new { Bucket = "31-60", Amount = b31_60 },
                    new { Bucket = "61-90", Amount = b61_90 },
                    new { Bucket = "90+",   Amount = b90p }
                }
            });
        }
    }
}
