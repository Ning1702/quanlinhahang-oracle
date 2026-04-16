using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quanlinhahang.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Quanlinhahang_Admin.Controllers
{
    public class ThongKeController : Controller
    {
        private readonly QuanLyNhaHangContext _ctx;
        public ThongKeController(QuanLyNhaHangContext ctx) => _ctx = ctx;

        public IActionResult Index() => View();

        // ============= 1) Doanh thu 12 tháng (Mốc năm 2026 hiện tại) =============
        [HttpGet]
        public async Task<IActionResult> RevenueLast12Months()
        {
            // Cập nhật mốc thời gian sang 2026 cho phù hợp hiện tại
            var anchorDate = new DateTime(2026, 12, 31);
            var start = new DateTime(anchorDate.Year, anchorDate.Month, 1).AddMonths(-11);

            var raw = await _ctx.HoaDons
                .Where(h => h.NgayLap >= start && h.NgayLap <= anchorDate)
                .ToListAsync();

            // Nhóm dữ liệu ở bộ nhớ để xử lý NgayLap nullable dễ dàng hơn
            var groupedRaw = raw
                .Where(h => h.NgayLap.HasValue)
                .GroupBy(h => new { h.NgayLap.Value.Year, h.NgayLap.Value.Month })
                .Select(g => new {
                    g.Key.Year,
                    g.Key.Month,
                    Total = g.Sum(x => x.TongTien ?? 0m)
                })
                .ToList();

            var result = Enumerable.Range(0, 12)
                .Select(i =>
                {
                    var d = start.AddMonths(i);
                    var hit = groupedRaw.FirstOrDefault(r => r.Year == d.Year && r.Month == d.Month);
                    return new
                    {
                        label = d.ToString("MM/yyyy"),
                        value = hit?.Total ?? 0m
                    };
                })
                .ToList();

            return Json(result);
        }

        // ============= 2) Doanh thu theo Quý (Năm 2026) =============
        [HttpGet]
        public async Task<IActionResult> RevenueQuarterAndYear(int? year)
        {
            int y = year ?? 2026;

            var raw = await _ctx.HoaDons
                .Where(h => h.NgayLap.HasValue && h.NgayLap.Value.Year == y)
                .Select(h => new { h.NgayLap.Value.Month, TongTien = h.TongTien ?? 0m })
                .ToListAsync();

            decimal q1 = raw.Where(x => x.Month <= 3).Sum(x => x.TongTien);
            decimal q2 = raw.Where(x => x.Month >= 4 && x.Month <= 6).Sum(x => x.TongTien);
            decimal q3 = raw.Where(x => x.Month >= 7 && x.Month <= 9).Sum(x => x.TongTien);
            decimal q4 = raw.Where(x => x.Month >= 10).Sum(x => x.TongTien);

            return Json(new { year = y, q1, q2, q3, q4, yearTotal = q1 + q2 + q3 + q4 });
        }

        // ============= 3) Tỷ lệ KH có tài khoản =============
        [HttpGet]
        public async Task<IActionResult> CustomerAccountPercent()
        {
            var total = await _ctx.KhachHangs.CountAsync();
            var haveAccount = await _ctx.KhachHangs.CountAsync(k => k.TaiKhoanId != null);

            var percent = total == 0 ? 0 : (double)haveAccount * 100.0 / total;
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
            var bestStats = await _ctx.HoaDons
                .Where(h => h.TaiKhoanId != null)
                .GroupBy(h => h.TaiKhoanId)
                .Select(g => new
                {
                    TaiKhoanID = g.Key,
                    TotalRevenue = g.Sum(x => x.TongTien ?? 0m),
                    CountOrders = g.Count()
                })
                .OrderByDescending(x => x.CountOrders)
                .FirstOrDefaultAsync();

            if (bestStats != null)
            {
                string name = "Không xác định";
                int? nvID = null;

                var nv = await _ctx.NhanViens.FirstOrDefaultAsync(n => n.TaiKhoanId == bestStats.TaiKhoanID);
                if (nv != null)
                {
                    name = nv.HoTen;
                    nvID = nv.NhanVienId;
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

        // ============= 5) Top 3 Khách hàng chi tiêu mạnh nhất =============
        [HttpGet]
        public async Task<IActionResult> TopCustomers()
        {
            var query = from hd in _ctx.HoaDons
                        join db in _ctx.DatBans on hd.DatBanId equals db.DatBanId
                        join kh in _ctx.KhachHangs on db.KhachHangId equals kh.KhachHangId
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