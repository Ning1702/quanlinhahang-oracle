using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;
using Quanlinhahang.Data.Models;
using System.Text;

namespace Quanlinhahang_Admin.Controllers
{
    public class AccountController : Controller
    {
        private readonly QuanLyNhaHangContext _context;

        public AccountController(QuanLyNhaHangContext context)
        {
            _context = context;
        }

        // ======================== LOGIN GET ========================
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Staff"))
                {
                    var userId = User.FindFirst("UserId")?.Value ?? "";
                    return Redirect($"https://localhost:7163/Auth/FromAdmin?userId={userId}");
                }
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            }
            return View();
        }

        // ======================== LOGIN POST ========================
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin!";
                return View();
            }

            // 1. Mã hóa mật khẩu
            string passHash = GetSHA256(password);

            // 2. Tìm user
            var user = _context.Taikhoans
                    .FirstOrDefault(t => t.Tendangnhap == username && t.Matkhauhash == passHash);

            if (user == null)
            {
                ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng!";
                return View();
            }

            // 3. Ghi Session
            HttpContext.Session.SetInt32("UserId", user.Taikhoanid);
            string role = user.Vaitro.ToString();

            // 4. Tạo Cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Tendangnhap),
                new Claim(ClaimTypes.Role, role),
                new Claim("UserId", user.Taikhoanid.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddMinutes(60)
                });

            // ================= PHÂN QUYỀN & CHUYỂN HƯỚNG =================

            // === TRƯỜNG HỢP 1: LÀ ADMIN ===
            if (role == "Admin")
            {
                return RedirectToAction("Index", "Home");
            }

            // === TRƯỜNG HỢP 2: LÀ STAFF ===
            if (role == "Staff")
            {
                string staffPort = "7163";
                string staffUrl = $"https://localhost:{staffPort}/Auth/FromAdmin?userId={user.Taikhoanid}";
                return Redirect(staffUrl);
            }

            ViewBag.Error = "Tài khoản không có quyền truy cập!";
            await HttpContext.SignOutAsync();
            return View();
        }

        // ======================== ACCESS DENIED (SỬA LỖI 404 HÌNH 4) ========================
        [HttpGet]
        public IActionResult AccessDenied()
        {
            // Trả về view thông báo lỗi thay vì trang trắng 404
            return View();
        }

        // ======================== LOGOUT ========================
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        public static string GetSHA256(string str)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] fromData = Encoding.UTF8.GetBytes(str);
                byte[] targetData = sha256.ComputeHash(fromData);
                return Convert.ToBase64String(targetData);
            }
        }
    }
}