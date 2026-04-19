using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quanlinhahang.Data.Models;
using Quanlinhahang_Staff.Models.ViewModels;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Quanlinhahang_Staff.Controllers
{
    public class TaiKhoanController : Controller
    {
        private readonly QuanLyNhaHangContext _context;
        private readonly IWebHostEnvironment _env;

        public TaiKhoanController(QuanLyNhaHangContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return Redirect("https://quanlinhahang-admin.onrender.com/Account/Login");

            var info = await (
                from nv in _context.NhanViens
                join tk in _context.TaiKhoans on nv.TaiKhoanId equals tk.TaiKhoanId
                where tk.TaiKhoanId == userId
                select new TaiKhoanStaffVM
                {
                    NhanVienID = nv.NhanVienId,
                    HoTen = nv.HoTen,
                    SoDienThoai = nv.SoDienThoai,
                    ChucVu = nv.ChucVu,
                    NgayVaoLam = nv.NgayVaoLam.HasValue
                        ? nv.NgayVaoLam.Value.ToDateTime(TimeOnly.MinValue)
                        : null,
                    TrangThaiNV = nv.TrangThai,
                    TaiKhoanID = tk.TaiKhoanId,
                    TenDangNhap = tk.TenDangNhap,
                    Email = tk.Email,
                    VaiTro = tk.VaiTro,
                    TrangThaiTK = tk.TrangThai,
                    MatKhauHash = tk.MatKhauHash
                }
            ).FirstOrDefaultAsync();

            if (info == null)
                return NotFound();

            return View(info);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhat(TaiKhoanStaffVM model, string? NewPassword)
        {
            int? sessionUserId = HttpContext.Session.GetInt32("UserId");
            if (sessionUserId == null)
                return Redirect("https://quanlinhahang-admin.onrender.com/Account/Login");

            var nv = await _context.NhanViens.FirstOrDefaultAsync(x => x.NhanVienId == model.NhanVienID);
            var tk = await _context.TaiKhoans.FirstOrDefaultAsync(x => x.TaiKhoanId == sessionUserId.Value);

            if (nv == null || tk == null)
            {
                TempData["update_error"] = true;
                TempData["msg"] = "Không tìm thấy thông tin tài khoản.";
                return RedirectToAction("Index");
            }

            string hoTenMoi = (model.HoTen ?? "").Trim();
            string sdtMoi = (model.SoDienThoai ?? "").Trim();
            string tenDangNhapMoi = (model.TenDangNhap ?? "").Trim();
            string emailMoi = (model.Email ?? "").Trim();

            if (string.IsNullOrWhiteSpace(hoTenMoi))
            {
                TempData["update_error"] = true;
                TempData["msg"] = "Họ tên không được để trống.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(tenDangNhapMoi))
            {
                TempData["update_error"] = true;
                TempData["msg"] = "Tên đăng nhập không được để trống.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(emailMoi))
            {
                TempData["update_error"] = true;
                TempData["msg"] = "Email không được để trống.";
                return RedirectToAction("Index");
            }

            bool tenDangNhapDaTonTai = await _context.TaiKhoans.AnyAsync(x =>
                x.TaiKhoanId != tk.TaiKhoanId &&
                x.TenDangNhap == tenDangNhapMoi);

            if (tenDangNhapDaTonTai)
            {
                TempData["update_error"] = true;
                TempData["msg"] = "Tên đăng nhập đã tồn tại.";
                return RedirectToAction("Index");
            }

            bool emailDaTonTai = await _context.TaiKhoans.AnyAsync(x =>
                x.TaiKhoanId != tk.TaiKhoanId &&
                x.Email == emailMoi);

            if (emailDaTonTai)
            {
                TempData["update_error"] = true;
                TempData["msg"] = "Email đã tồn tại.";
                return RedirectToAction("Index");
            }

            // Cập nhật dữ liệu
            nv.HoTen = hoTenMoi;
            nv.SoDienThoai = sdtMoi;

            tk.TenDangNhap = tenDangNhapMoi;
            tk.Email = emailMoi;

            if (!string.IsNullOrWhiteSpace(NewPassword) && NewPassword != "********")
            {
                tk.MatKhauHash = GetSHA256(NewPassword.Trim());
            }

            var changed = await _context.SaveChangesAsync();

            if (changed <= 0)
            {
                TempData["update_error"] = true;
                TempData["msg"] = "Không có dữ liệu nào được cập nhật.";
                return RedirectToAction("Index");
            }

            // Cập nhật lại session nếu cần
            HttpContext.Session.SetInt32("UserId", tk.TaiKhoanId);

            // Refresh cookie đăng nhập để claim / tên mới đồng bộ ngay
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, tk.TaiKhoanId.ToString()),
                new Claim(ClaimTypes.Name, tk.TenDangNhap ?? ""),
                new Claim(ClaimTypes.Email, tk.Email ?? ""),
                new Claim(ClaimTypes.Role, tk.VaiTro ?? "Staff")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = false,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });

            TempData["update"] = true;
            TempData["msg"] = "Cập nhật thông tin cá nhân thành công!";
            return RedirectToAction("Index");
        }

        private string GetSHA256(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(str);
                byte[] hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();

            return Redirect("https://quanlinhahang-admin.onrender.com/Account/Login");
        }

        [HttpGet]
        [Route("AccessDenied")]
        public IActionResult AccessDenied()
        {
            ViewBag.Message = "Bạn không có quyền truy cập vào trang này!";
            return View();
        }
    }
}