using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Quanlinhahang_Staff.Controllers
{
    public class AuthController : Controller
    {
        [HttpGet]
        public IActionResult FromAdmin(int userId)
        {
            // 1. Lưu session xác nhận nhân viên đã đăng nhập
            HttpContext.Session.SetInt32("UserId", userId);

            // 2. Chuyển hướng vào trang quản lý hóa đơn (Invoices/Index)
            return RedirectToAction("Index", "Invoices");
        }
    }
}