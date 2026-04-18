using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quanlinhahang.Data.Models;

namespace Quanlinhahang_Admin.Controllers
{
    public class ThongKeController : Controller
    {
        private readonly QuanLyNhaHangContext _ctx;

        public ThongKeController(QuanLyNhaHangContext ctx)
        {
            _ctx = ctx;
        }

        public IActionResult Index() => View();

        // ============= 1) Doanh thu 12 tháng gần nhất =============
        [HttpGet]
        public async Task<IActionResult> RevenueLast12Months()
        {
            var paidInvoices = await _ctx.HoaDons
                .AsNoTracking()
                .Where(h => h.TrangThaiId == 4 && h.NgayLap.HasValue)
                .Select(h => new
                {
                    NgayLap = h.NgayLap!.Value,
                    TongTien = h.TongTien ?? 0m
                })
                .ToListAsync();

            if (!paidInvoices.Any())
            {
                var empty = Enumerable.Range(0, 12)
                    .Select(i => new
                    {
                        label = DateTime.UtcNow.AddMonths(i - 11).ToString("MM/yyyy"),
                        value = 0m
                    })
                    .ToList();

                return Json(empty);
            }

            var anchorDate = paidInvoices.Max(x => x.NgayLap);
            var start = new DateTime(anchorDate.Year, anchorDate.Month, 1).AddMonths(-11);
            var end = new DateTime(anchorDate.Year, anchorDate.Month, 1).AddMonths(1).AddTicks(-1);

            var grouped = paidInvoices
                .Where(x => x.NgayLap >= start && x.NgayLap <= end)
                .GroupBy(x => new { x.NgayLap.Year, x.NgayLap.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Total = g.Sum(x => x.TongTien)
                })
                .ToList();

            var result = Enumerable.Range(0, 12)
                .Select(i =>
                {
                    var d = start.AddMonths(i);
                    var hit = grouped.FirstOrDefault(r => r.Year == d.Year && r.Month == d.Month);
                    return new
                    {
                        label = d.ToString("MM/yyyy"),
                        value = hit?.Total ?? 0m
                    };
                })
                .ToList();

            return Json(result);
        }

        // ============= 2) Doanh thu theo Quý và cả năm =============
        [HttpGet]
        public async Task<IActionResult> RevenueQuarterAndYear(int? year)
        {
            var paidInvoices = await _ctx.HoaDons
                .AsNoTracking()
                .Where(h => h.TrangThaiId == 4 && h.NgayLap.HasValue)
                .Select(h => new
                {
                    NgayLap = h.NgayLap!.Value,
                    TongTien = h.TongTien ?? 0m
                })
                .ToListAsync();

            int y = year ?? (paidInvoices.Any() ? paidInvoices.Max(x => x.NgayLap.Year) : DateTime.UtcNow.Year);

            var raw = paidInvoices
                .Where(x => x.NgayLap.Year == y)
                .Select(x => new
                {
                    Month = x.NgayLap.Month,
                    x.TongTien
                })
                .ToList();

            decimal q1 = raw.Where(x => x.Month >= 1 && x.Month <= 3).Sum(x => x.TongTien);
            decimal q2 = raw.Where(x => x.Month >= 4 && x.Month <= 6).Sum(x => x.TongTien);
            decimal q3 = raw.Where(x => x.Month >= 7 && x.Month <= 9).Sum(x => x.TongTien);
            decimal q4 = raw.Where(x => x.Month >= 10 && x.Month <= 12).Sum(x => x.TongTien);

            return Json(new
            {
                year = y,
                q1,
                q2,
                q3,
                q4,
                yearTotal = q1 + q2 + q3 + q4
            });
        }

        // ============= 3) Tỷ lệ KH có tài khoản =============
        [HttpGet]
        public async Task<IActionResult> CustomerAccountPercent()
        {
            var total = await _ctx.KhachHangs.AsNoTracking().CountAsync();
            var haveAccount = await _ctx.KhachHangs.AsNoTracking().CountAsync(k => k.TaiKhoanId != null);

            var percent = total == 0 ? 0 : (double)haveAccount * 100.0 / total;

            return Json(new
            {
                totalKH = total,
                coTaiKhoan = haveAccount,
                percent = Math.Round(percent, 2)
            });
        }

        // ============= 4) Nhân viên xuất sắc nhất =============
        [HttpGet]
        public async Task<IActionResult> BestEmployee()
        {
            var bestStats = await _ctx.HoaDons
                .AsNoTracking()
                .Where(h => h.TaiKhoanId != null && h.TrangThaiId == 4)
                .GroupBy(h => h.TaiKhoanId)
                .Select(g => new
                {
                    TaiKhoanID = g.Key,
                    TotalRevenue = g.Sum(x => x.TongTien ?? 0m),
                    CountOrders = g.Count()
                })
                .OrderByDescending(x => x.CountOrders)
                .ThenByDescending(x => x.TotalRevenue)
                .FirstOrDefaultAsync();

            if (bestStats != null)
            {
                var nv = await _ctx.NhanViens
                    .AsNoTracking()
                    .FirstOrDefaultAsync(n => n.TaiKhoanId == bestStats.TaiKhoanID);

                return Json(new
                {
                    HoTen = nv?.HoTen ?? "Không xác định",
                    NhanVienID = nv?.NhanVienId,
                    bestStats.TotalRevenue,
                    bestStats.CountOrders
                });
            }

            return Json(new
            {
                HoTen = "Chưa có dữ liệu",
                NhanVienID = (int?)null,
                TotalRevenue = 0m,
                CountOrders = 0
            });
        }

        // ============= 5) Top 3 khách hàng chi tiêu mạnh nhất =============
        [HttpGet]
        public async Task<IActionResult> TopCustomers()
        {
            var query = from hd in _ctx.HoaDons.AsNoTracking()
                        join db in _ctx.DatBans.AsNoTracking() on hd.DatBanId equals db.DatBanId
                        join kh in _ctx.KhachHangs.AsNoTracking() on db.KhachHangId equals kh.KhachHangId
                        where hd.TrangThaiId == 4
                        group hd by new { kh.KhachHangId, kh.HoTen, kh.SoDienThoai } into g
                        select new
                        {
                            TenKhachHang = g.Key.HoTen,
                            SoDienThoai = g.Key.SoDienThoai,
                            TotalSpent = g.Sum(x => x.TongTien ?? 0m)
                        };

            var top3 = await query
                .OrderByDescending(x => x.TotalSpent)
                .Take(3)
                .ToListAsync();

            return Json(top3);
        }
    }
}