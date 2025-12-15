using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Cần thêm để dùng các hàm async nếu muốn
using Quanlinhahang.Data.Models;
using Quanlinhahang_Staff.Models.ViewModels;
using System;
using System.Linq;
using System.Security.Claims; // Cần thêm để lấy User ID từ đăng nhập

namespace Quanlinhahang_Staff.Controllers
{
    public class ReportsController : Controller
    {
        private readonly QuanLyNhaHangContext _context;

        public ReportsController(QuanLyNhaHangContext context)
        {
            _context = context;
        }

        // GET: /Reports
        public IActionResult Index()
        {
            // 1. Lấy ID tài khoản đang đăng nhập (thay vì fix cứng = 1)
            // Lưu ý: Bạn cần đảm bảo đã lưu ClaimTypes.NameIdentifier khi Login
            int currentTaiKhoanId = 1; // Giá trị mặc định nếu chưa đăng nhập

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int parsedId))
            {
                currentTaiKhoanId = parsedId;
            }

            // 2. Chuẩn bị mốc thời gian
            DateTime today = DateTime.Today;
            DateTime startOfToday = today;
            DateTime endOfToday = today.AddDays(1); // Dùng khoảng thời gian >= và < để an toàn hơn so với .Date
            DateTime firstDayOfMonth = new DateTime(today.Year, today.Month, 1);

            // 3. Truy vấn dữ liệu (LINQ)
            // Lọc chung cho nhân viên này để code gọn hơn
            var query = _context.Hoadons.Where(h => h.Taikhoanid == currentTaiKhoanId);

            // Tổng hóa đơn trong ngày
            var totalToday = query.Count(h => h.Ngaylap >= startOfToday && h.Ngaylap < endOfToday);

            // Tổng hóa đơn trong tháng
            var totalMonth = query.Count(h => h.Ngaylap >= firstDayOfMonth);

            // ID trạng thái đã thanh toán (giả sử là 4 như code cũ của bạn)
            int trangThaiDaThanhToan = 4;

            // Tổng doanh thu (Chỉ tính những đơn đã thanh toán)
            // Dùng (decimal?) cast để tránh lỗi nếu không có dòng nào (kết quả null -> về 0)
            var doanhThu = query
                .Where(h => h.Trangthaiid == trangThaiDaThanhToan)
                .Sum(h => (decimal?)h.Tongtien) ?? 0;

            // Hoa hồng: 5% doanh thu
            var hoaHong = doanhThu * 0.05m;

            // Hóa đơn đã thanh toán
            var daTT = query.Count(h => h.Trangthaiid == trangThaiDaThanhToan);

            // Hóa đơn chưa thanh toán (khác trạng thái 4)
            var chuaTT = query.Count(h => h.Trangthaiid != trangThaiDaThanhToan);

            // 4. Đổ dữ liệu vào ViewModel
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