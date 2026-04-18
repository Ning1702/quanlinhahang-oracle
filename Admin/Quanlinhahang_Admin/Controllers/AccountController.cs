using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quanlinhahang.Data.Models;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

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
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Vui lòng nhập tên đăng nhập và mật khẩu!";
                return View();
            }

            string inputHash = GetSHA256(password);

            var user = await _context.TaiKhoans
                .FirstOrDefaultAsync(t => t.TenDangNhap == username && t.MatKhauHash == inputHash);

            if (user == null)
            {
                ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng!";
                return View();
            }

            if (!string.IsNullOrWhiteSpace(user.TrangThai) &&
                !user.TrangThai.Equals("Hoạt động", StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.Error = "Tài khoản của bạn hiện không hoạt động!";
                return View();
            }

            string dbRoleRaw = user.VaiTro ?? string.Empty;
            string finalRole;

            if (dbRoleRaw.Contains("Admin", StringComparison.OrdinalIgnoreCase) ||
                dbRoleRaw.Contains("Quản lý", StringComparison.OrdinalIgnoreCase))
            {
                finalRole = "Admin";
            }
            else if (dbRoleRaw.Contains("Staff", StringComparison.OrdinalIgnoreCase) ||
                     dbRoleRaw.Contains("Nhân viên", StringComparison.OrdinalIgnoreCase))
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
                new Claim(ClaimTypes.Name, user.TenDangNhap ?? string.Empty),
                new Claim("UserId", user.TaiKhoanId.ToString()),
                new Claim(ClaimTypes.Role, finalRole)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);

            if (finalRole == "Admin")
            {
                return RedirectToAction("Index", "Home");
            }

            if (finalRole == "Staff")
            {
                string staffBaseUrl = _configuration["AppUrls:StaffUrl"] ?? string.Empty;

                if (string.IsNullOrWhiteSpace(staffBaseUrl))
                {
                    staffBaseUrl = "https://quanlinhahang-staff.onrender.com";
                }

                staffBaseUrl = staffBaseUrl.TrimEnd('/');

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

        [HttpGet]
        public IActionResult AccessDenied()
        {
            ViewBag.Message = "Bạn không có quyền truy cập vào trang này!";
            return View();
        }

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