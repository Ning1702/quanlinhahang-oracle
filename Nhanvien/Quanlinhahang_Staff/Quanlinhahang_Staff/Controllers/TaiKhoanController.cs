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
            if (!ModelState.IsValid)
                return View("Index", model);

            var nv = await _context.NhanViens.FirstOrDefaultAsync(x => x.NhanVienId == model.NhanVienID);
            var tk = await _context.TaiKhoans.FirstOrDefaultAsync(x => x.TaiKhoanId == model.TaiKhoanID);

            if (nv == null || tk == null)
                return NotFound();

            string tenDangNhapMoi = (model.TenDangNhap ?? "").Trim();
            string emailMoi = (model.Email ?? "").Trim();
            string soDienThoaiMoi = (model.SoDienThoai ?? "").Trim();

            if (string.IsNullOrWhiteSpace(model.HoTen))
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
                x.TaiKhoanId != model.TaiKhoanID &&
                x.TenDangNhap == tenDangNhapMoi);

            if (tenDangNhapDaTonTai)
            {
                TempData["update_error"] = true;
                TempData["msg"] = "Tên đăng nhập đã tồn tại.";
                return RedirectToAction("Index");
            }

            bool emailDaTonTai = await _context.TaiKhoans.AnyAsync(x =>
                x.TaiKhoanId != model.TaiKhoanID &&
                x.Email == emailMoi);

            if (emailDaTonTai)
            {
                TempData["update_error"] = true;
                TempData["msg"] = "Email đã tồn tại.";
                return RedirectToAction("Index");
            }

            nv.HoTen = model.HoTen.Trim();
            nv.SoDienThoai = soDienThoaiMoi;

            tk.TenDangNhap = tenDangNhapMoi;
            tk.Email = emailMoi;

            if (!string.IsNullOrWhiteSpace(NewPassword) && NewPassword != "********")
            {
                tk.MatKhauHash = GetSHA256(NewPassword.Trim());
            }

            await _context.SaveChangesAsync();

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