using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quanlinhahang.Data.Models;

namespace Quanlinhahang_Customer.Controllers
{
    [Route("testdb")]
    public class TestDbController : Controller
    {
        private readonly QuanLyNhaHangContext _context;

        public TestDbController(QuanLyNhaHangContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                bool canConnect = await _context.Database.CanConnectAsync();

                if (!canConnect)
                {
                    return Content("❌ KHÔNG THỂ KẾT NỐI TỚI ORACLE (CanConnect = false). Kiểm tra lại Connection String, IP, Tường lửa.");
                }

                var countKH = await _context.KhachHangs.CountAsync();

                var countTK = await _context.TaiKhoans.CountAsync();

                return Content($"✅ KẾT NỐI THÀNH CÔNG!\n" +
                               $"- Kết nối DB: OK\n" +
                               $"- Số lượng Khách hàng: {countKH}\n" +
                               $"- Số lượng Tài khoản: {countTK}\n\n" +
                               $"Nếu bạn thấy dòng này, Database đã hoạt động tốt.");
            }
            catch (Exception ex)
            {
                // In ra toàn bộ lỗi chi tiết
                string errorMsg = $"❌ LỖI KẾT NỐI:\n\nMessage: {ex.Message}\n\nInner Exception: {ex.InnerException?.Message}\n\nStack Trace: {ex.StackTrace}";
                return Content(errorMsg);
            }
        }
    }
}