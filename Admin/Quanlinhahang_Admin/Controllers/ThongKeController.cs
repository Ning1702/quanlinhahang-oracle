using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quanlinhahang.Data.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Quanlinhahang_Admin.Controllers
{
    public class ThongKeController : Controller
    {
        private readonly QuanLyNhaHangContext _ctx;
        public ThongKeController(QuanLyNhaHangContext ctx) => _ctx = ctx;

        public IActionResult Index() => View();

        // ============= 1) Doanh thu 12 tháng (Neo mốc năm 2025) =============
        [HttpGet]
        public async Task<IActionResult> RevenueLast12Months()
        {
            var anchorDate = new DateTime(2025, 12, 31);
            var start = new DateTime(anchorDate.Year, anchorDate.Month, 1).AddMonths(-11);

            var raw = await _ctx.Hoadons
                .Where(h => h.Ngaylap >= start && h.Ngaylap <= anchorDate)
                .GroupBy(h => new { h.Ngaylap.Year, h.Ngaylap.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(x => x.Tongtien) })
                .ToListAsync();

            var result = Enumerable.Range(0, 12)
                .Select(i =>
                {
                    var d = start.AddMonths(i);
                    var hit = raw.FirstOrDefault(r => r.Year == d.Year && r.Month == d.Month);
                    return new
                    {
                        label = d.ToString("MM/yyyy"),
                        value = hit?.Total ?? 0m
                    };
                })
                .ToList();

            return Json(result);
        }

        // ============= 2) Doanh thu theo Quý (Mặc định năm 2025) =============
        [HttpGet]
        public async Task<IActionResult> RevenueQuarterAndYear(int? year)
        {
            int y = year ?? 2025;

            var raw = await _ctx.Hoadons
                .Where(h => h.Ngaylap.Year == y)
                .Select(h => new { h.Ngaylap.Month, h.Tongtien })
                .ToListAsync();

            decimal q1 = raw.Where(x => x.Month <= 3).Sum(x => x.Tongtien);
            decimal q2 = raw.Where(x => x.Month >= 4 && x.Month <= 6).Sum(x => x.Tongtien);
            decimal q3 = raw.Where(x => x.Month >= 7 && x.Month <= 9).Sum(x => x.Tongtien);
            decimal q4 = raw.Where(x => x.Month >= 10).Sum(x => x.Tongtien);

            return Json(new { year = y, q1, q2, q3, q4, yearTotal = q1 + q2 + q3 + q4 });
        }

        // ============= 3) Tỷ lệ KH có tài khoản =============
        [HttpGet]
        public async Task<IActionResult> CustomerAccountPercent()
        {
            // [SỬA]: Khachhangs, Taikhoanid
            var total = await _ctx.Khachhangs.CountAsync();
            var haveAccount = await _ctx.Khachhangs.CountAsync(k => k.Taikhoanid != null);

            var percent = total == 0 ? 0 : haveAccount * 100.0 / total;
            return Json(new
            {
                totalKH = total,
                coTaiKhoan = haveAccount,
                percent = Math.Round(percent, 2)
            });
        }

        // ============= 4) Nhân viên xuất sắc nhất (theo số đơn) =============
        [HttpGet]
        public async Task<IActionResult> BestEmployee()
        {
            var bestStats = await _ctx.Hoadons
                .Where(h => h.Taikhoanid != null)
                .GroupBy(h => h.Taikhoanid)
                .Select(g => new
                {
                    TaiKhoanID = g.Key,
                    TotalRevenue = g.Sum(x => x.Tongtien),
                    CountOrders = g.Count()
                })
                .OrderByDescending(x => x.CountOrders)
                .FirstOrDefaultAsync();

            if (bestStats != null)
            {
                string name = "Không xác định";
                int? nvID = null;

                // [SỬA]: Nhanviens, Hoten, Nhanvienid
                var nv = await _ctx.Nhanviens.FirstOrDefaultAsync(n => n.Taikhoanid == bestStats.TaiKhoanID);
                if (nv != null)
                {
                    name = nv.Hoten;
                    nvID = nv.Nhanvienid;
                }

                return Json(new
                {
                    HoTen = name,
                    NhanVienID = nvID,
                    bestStats.TotalRevenue,
                    bestStats.CountOrders
                });
            }

            return Json(new
            {
                HoTen = "Chưa có dữ liệu",
                NhanVienID = "-",
                TotalRevenue = 0m,
                CountOrders = 0
            });
        }

        // ============= 5) Top 3 Khách hàng =============
        [HttpGet]
        public async Task<IActionResult> TopCustomers()
        {
            // [SỬA]: Datbans, Khachhangs, Hoadons, Hoten, Sodienthoai, Tongtien
            var query = from hd in _ctx.Hoadons
                        join db in _ctx.Datbans on hd.Datbanid equals db.Datbanid
                        join kh in _ctx.Khachhangs on db.Khachhangid equals kh.Khachhangid
                        group hd by new { kh.Khachhangid, kh.Hoten, kh.Sodienthoai } into g
                        select new
                        {
                            TenKhachHang = g.Key.Hoten,
                            g.Key.Sodienthoai,
                            TotalSpent = g.Sum(x => x.Tongtien)
                        };

            var top3 = await query
                .OrderByDescending(x => x.TotalSpent)
                .Take(3)
                .ToListAsync();

            return Json(top3);
        }
    }
}
