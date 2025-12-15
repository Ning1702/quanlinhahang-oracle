using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Quanlinhahang.Data.Models; // Correct Data Model Namespace
using Quanlinhahang_Staff.Models.ViewModels;
using System.Security.Cryptography;
using System.Text;

namespace Quanlinhahang_Staff.Controllers
{
    public class TaiKhoanController : Controller
    {
        private readonly QuanLyNhaHangContext _context; // Use the correct Context class
        private readonly IWebHostEnvironment _env;

        public TaiKhoanController(QuanLyNhaHangContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ============================================================
        // 1. HIỂN THỊ THÔNG TIN
        // ============================================================
        public IActionResult Index()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return Redirect("https://localhost:7011/Account/Login"); // Ensure port 7011 is correct for Admin

            // LINQ Query updated for Oracle case-sensitivity (PascalCase typically generated)
            var info = (
                from nv in _context.Nhanviens // Table: Nhanviens
                join tk in _context.Taikhoans on nv.Taikhoanid equals tk.Taikhoanid // Join keys: Taikhoanid
                where tk.Taikhoanid == userId
                select new TaiKhoanStaffVM
                {
                    // Map Entity properties (PascalCase) to ViewModel
                    NhanVienID = nv.Nhanvienid,
                    HoTen = nv.Hoten,
                    SoDienThoai = nv.Sodienthoai,
                    ChucVu = nv.Chucvu,
                    NgayVaoLam = nv.Ngayvaolam,
                    TrangThaiNV = nv.Trangthai,

                    TaiKhoanID = tk.Taikhoanid,
                    TenDangNhap = tk.Tendangnhap,
                    Email = tk.Email,
                    // Convert Enum to String for display/ViewModel
                    VaiTro = tk.Vaitro.ToString(),
                    TrangThaiTK = tk.Trangthai,
                    MatKhauHash = tk.Matkhauhash
                }
            ).FirstOrDefault();

            if (info == null)
                return NotFound();

            return View(info);
        }

        // ============================================================
        // 2. CẬP NHẬT THÔNG TIN (GỘP CẢ ĐỔI MẬT KHẨU)
        // ============================================================
        [HttpPost]
        public IActionResult CapNhat(TaiKhoanStaffVM model, string NewPassword)
        {
            var nv = _context.Nhanviens.FirstOrDefault(x => x.Nhanvienid == model.NhanVienID);
            if (nv != null)
            {
                nv.Hoten = model.HoTen ?? "";
                nv.Sodienthoai = model.SoDienThoai ?? "";
            }

            var tk = _context.Taikhoans.FirstOrDefault(x => x.Taikhoanid == model.TaiKhoanID);
            if (tk != null)
            {
                tk.Email = model.Email ?? "";

                if (!string.IsNullOrEmpty(NewPassword) && NewPassword != "********")
                {
                    tk.Matkhauhash = GetSHA256(NewPassword);
                }
            }

            _context.SaveChanges();

            TempData["update"] = true;
            TempData["msg"] = "Cập nhật thông tin thành công!";
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
            // 1. Xóa Cookie
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // 2. Xóa Session
            HttpContext.Session.Clear();

            // 3. Chuyển hướng về Admin Login
            return Redirect("https://localhost:7011/Account/Login");
        }
    }
}