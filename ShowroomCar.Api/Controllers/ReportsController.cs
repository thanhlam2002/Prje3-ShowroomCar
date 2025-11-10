using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShowroomCar.Infrastructure.Persistence.Entities;

namespace ShowroomCar.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "RequireAdmin")]
    public class ReportsController : ControllerBase
    {
        private readonly ShowroomDbContext _db;
        public ReportsController(ShowroomDbContext db) => _db = db;

        // 1️⃣ Báo cáo tồn kho
        [HttpGet("inventory")]
        public async Task<ActionResult<object>> InventorySummary()
        {
            var data = await _db.Vehicles
                .GroupBy(v => v.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();
            return Ok(data);
        }

        // 2️⃣ Báo cáo doanh thu theo tháng
        [HttpGet("revenue")]
        public async Task<ActionResult<object>> MonthlyRevenue()
        {
            var raw = await _db.Invoices
                .Where(i => i.Status == "PAID_FULL" || i.Status == "PAID_PARTIAL")
                .GroupBy(i => new { i.InvoiceDate.Year, i.InvoiceDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Total = g.Sum(i => i.GrandTotal)
                })
                .ToListAsync(); // <-- EF Core vẫn hiểu đến đây, vẫn dùng ToListAsync được

            // Phần định dạng chuỗi làm ở bộ nhớ, không còn lỗi string.Format
            var data = raw
                .AsEnumerable() // chuyển sang client side
                .Select(x => new
                {
                    Period = $"{x.Year}-{x.Month:D2}",
                    x.Total
                })
                .OrderBy(x => x.Period)
                .ToList(); // ⬅ đổi thành ToList (không Async nữa)

            return Ok(data);
        }



        // 3️⃣ Báo cáo công nợ (AR Aging)
        [HttpGet("aging")]
        public async Task<ActionResult<object>> Aging()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var invoices = await _db.Invoices
                .AsNoTracking()
                .Where(i => i.Status != "PAID_FULL")
                .Select(i => new
                {
                    i.InvoiceId,
                    i.InvoiceNo,
                    i.CustomerId,
                    i.GrandTotal,
                    i.InvoiceDate
                })
                .ToListAsync();

            var allocs = await _db.PaymentAllocations
                .GroupBy(a => a.InvoiceId)
                .Select(g => new { InvoiceId = g.Key, Paid = g.Sum(a => a.AmountApplied) })
                .ToListAsync();

            var result = invoices.Select(i =>
            {
                var paid = allocs.FirstOrDefault(a => a.InvoiceId == i.InvoiceId)?.Paid ?? 0;
                var due = i.GrandTotal - paid;
                var days = (today.ToDateTime(TimeOnly.MinValue) - i.InvoiceDate.ToDateTime(TimeOnly.MinValue)).Days;

                string bucket = days <= 30 ? "0–30 ngày"
                              : days <= 60 ? "31–60 ngày"
                              : ">60 ngày";

                return new
                {
                    i.InvoiceNo,
                    i.CustomerId,
                    i.GrandTotal,
                    Paid = paid,
                    Due = due,
                    AgeDays = days,
                    Bucket = bucket
                };
            });

            return Ok(result);
        }

        // 4️⃣ Top khách hàng theo doanh thu
        [HttpGet("top-customers")]
        public async Task<ActionResult<object>> TopCustomers()
        {
            var data = await _db.Invoices
                .Where(i => i.Status == "PAID_FULL")
                .GroupBy(i => i.CustomerId)
                .Select(g => new
                {
                    CustomerId = g.Key,
                    TotalSpent = g.Sum(i => i.GrandTotal)
                })
                .OrderByDescending(x => x.TotalSpent)
                .Take(10)
                .ToListAsync();

            var enriched = await _db.Customers
                .Where(c => data.Select(x => x.CustomerId).Contains(c.CustomerId))
                .Select(c => new
                {
                    c.CustomerId,
                    c.FullName
                })
                .ToListAsync();

            var result = from d in data
                         join c in enriched on d.CustomerId equals c.CustomerId
                         select new { c.FullName, d.TotalSpent };

            return Ok(result);
        }
    }
}
