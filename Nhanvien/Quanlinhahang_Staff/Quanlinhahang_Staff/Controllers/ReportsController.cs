using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quanlinhahang.Data.Models;
using Quanlinhahang_Staff.Models.ViewModels;
using System;
using System.Linq;
using System.Security.Claims; 

namespace Quanlinhahang_Staff.Controllers
{
    public class ReportsController : BaseController
    {
        private readonly QuanLyNhaHangContext _context;

        public ReportsController(QuanLyNhaHangContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            if (!IsLoggedIn) return RequireLogin();
            int currentTaiKhoanId = CurrentUserId.Value;

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int parsedId))
            {
                currentTaiKhoanId = parsedId;
            }

            DateTime today = DateTime.Today;
            DateTime startOfToday = today;
            DateTime endOfToday = today.AddDays(1);
            DateTime firstDayOfMonth = new DateTime(today.Year, today.Month, 1);

            var query = _context.HoaDons.Where(h => h.TaiKhoanId == currentTaiKhoanId);

            var totalToday = query.Count(h => h.NgayLap >= startOfToday && h.NgayLap < endOfToday);

            var totalMonth = query.Count(h => h.NgayLap >= firstDayOfMonth);

            int trangThaiDaThanhToan = 4;

            var doanhThu = query
                .Where(h => h.TrangThaiId == trangThaiDaThanhToan)
                .Sum(h => (decimal?)h.TongTien) ?? 0;

            var hoaHong = doanhThu * 0.05m;

            var daTT = query.Count(h => h.TrangThaiId == trangThaiDaThanhToan);

            var chuaTT = query.Count(h => h.TrangThaiId != trangThaiDaThanhToan);

            var vm = new ReportVM
            {
                TongHoaDonHomNay = totalToday,
                TongHoaDonThangNay = totalMonth,
                TongDoanhThu = doanhThu,
                HoaHong = hoaHong,
                SoHoaDonDaThanhToan = daTT,
                SoHoaDonChuaThanhToan = chuaTT
            };

            return View(vm);
        }
    }
}