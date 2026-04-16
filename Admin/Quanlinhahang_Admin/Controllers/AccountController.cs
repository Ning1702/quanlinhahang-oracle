using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Quanlinhahang.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Quanlinhahang_Admin.Controllers
{
    public class AccountController : Controller
    {
        private readonly QuanLyNhaHangContext _context;
        private readonly IConfiguration _configuration;

        public AccountController(QuanLyNhaHangContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // Nếu đã đăng nhập thì đẩy về Home luôn
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập tên đăng nhập và mật khẩu!";
                return View();
            }

            string inputHash = GetSHA256(password);

            // Cập nhật: PascalCase cho các thuộc tính TaiKhoans
            var user = await _context.TaiKhoans
                .FirstOrDefaultAsync(t => t.TenDangNhap == username && t.MatKhauHash == inputHash);

            if (user == null)
            {
                ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng!";
                return View();
            }

            // ============================================================
            // KIỂM TRA QUYỀN TRUY CẬP (Dựa trên chuỗi trong SQL Server)
            // ============================================================
            string dbRoleRaw = user.VaiTro ?? "";
            string finalRole = "";

            // Kiểm tra nếu là Admin hoặc Quản lý
            if (dbRoleRaw.Contains("Admin") || dbRoleRaw.Contains("Quản lý"))
            {
                finalRole = "Admin";
            }
            // Kiểm tra nếu là Nhân viên/Staff
            else if (dbRoleRaw.Contains("Staff") || dbRoleRaw.Contains("Nhân viên"))
            {
                finalRole = "Staff";
            }
            else
            {
                ViewBag.Error = "Tài khoản khách hàng không có quyền truy cập trang quản trị!";
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.TenDangNhap),
                new Claim("UserId", user.TaiKhoanId.ToString()),
                new Claim(ClaimTypes.Role, finalRole)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // Thiết lập Cookie đăng nhập
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // ============================================================
            // ĐIỀU HƯỚNG DỰA TRÊN ROLE
            // ============================================================
            if (finalRole == "Admin")
            {
                // Admin ở lại trang quản trị hiện tại
                return RedirectToAction("Index", "Home");
            }
            else if (finalRole == "Staff")
            {
                // Staff chuyển sang dự án Staff chuyên dụng (StaffUrl cấu hình trong appsettings.json)
                string staffBaseUrl = _configuration["AppUrls:StaffUrl"];

                if (string.IsNullOrEmpty(staffBaseUrl))
                {
                    staffBaseUrl = "https://localhost:7163"; // Port mặc định dự án Staff của bạn
                }

                // Redirect sang link xác thực trung gian giữa Admin và Staff
                string redirectUrl = $"{staffBaseUrl}/Auth/FromAdmin?userId={user.TaiKhoanId}";
                return Redirect(redirectUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // Hàm băm mật khẩu SHA256 đồng bộ toàn hệ thống
        public static string GetSHA256(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] fromData = Encoding.UTF8.GetBytes(str);
                byte[] targetData = sha256.ComputeHash(fromData);
                return Convert.ToBase64String(targetData);
            }
        }
    }
}