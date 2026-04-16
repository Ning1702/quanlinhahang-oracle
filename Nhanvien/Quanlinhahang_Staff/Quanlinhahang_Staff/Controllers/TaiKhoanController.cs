using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quanlinhahang.Data.Models;
using Quanlinhahang_Staff.Models.ViewModels;
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

        // ============================================================
        // 1. HIỂN THỊ THÔNG TIN TÀI KHOẢN NHÂN VIÊN
        // ============================================================
        public async Task<IActionResult> Index()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

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

        // ============================================================
        // 2. CẬP NHẬT THÔNG TIN (GỘP CẢ ĐỔI MẬT KHẨU)
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> CapNhat(TaiKhoanStaffVM model, string NewPassword)
        {
            var nv = await _context.NhanViens.FirstOrDefaultAsync(x => x.NhanVienId == model.NhanVienID);
            if (nv != null)
            {
                nv.HoTen = model.HoTen ?? "";
                nv.SoDienThoai = model.SoDienThoai ?? "";
            }

            var tk = await _context.TaiKhoans.FirstOrDefaultAsync(x => x.TaiKhoanId == model.TaiKhoanID);
            if (tk != null)
            {
                tk.Email = model.Email ?? "";

                if (!string.IsNullOrEmpty(NewPassword) && NewPassword != "********")
                {
                    tk.MatKhauHash = GetSHA256(NewPassword);
                }
            }

            await _context.SaveChangesAsync();

            TempData["update"] = true;
            TempData["msg"] = "Cập nhật thông tin cá nhân thành công!";
            return RedirectToAction("Index");
        }

        // ============================================================
        // 3. HÀM MÃ HÓA SHA256 (Helper)
        // ============================================================
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

        // ======================== LOGOUT ========================
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();

            return RedirectToAction("Login", "Account");
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