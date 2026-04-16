using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quanlinhahang.Data.Models;
using System.Security.Cryptography;
using System.Text;

namespace Quanlinhahang_Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class TaiKhoansController : Controller
    {
        private readonly QuanLyNhaHangContext _context;
        public TaiKhoansController(QuanLyNhaHangContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _context.TaiKhoans
                // Đã sửa: So sánh trực tiếp với chuỗi "Customer" thay vì Enum
                .Where(t => t.VaiTro != "Customer")
                .OrderBy(t => t.VaiTro)
                .ToListAsync();

            return View(data);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaiKhoan model, string MatKhau)
        {
            if (await _context.TaiKhoans.AnyAsync(t => t.TenDangNhap == model.TenDangNhap))
            {
                ModelState.AddModelError("TenDangNhap", "Tên đăng nhập đã tồn tại");
                return View(model);
            }

            model.MatKhauHash = HashPassword(MatKhau);

            model.TrangThai = "Hoạt động";

            _context.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var tk = await _context.TaiKhoans.FindAsync(id);
            if (tk == null) return NotFound();
            return View(tk);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TaiKhoan model, string? NewPassword)
        {
            if (id != model.TaiKhoanId) return NotFound();

            var tk = await _context.TaiKhoans.FindAsync(id);
            if (tk == null) return NotFound();

            tk.Email = model.Email;
            tk.VaiTro = model.VaiTro;
            tk.TrangThai = model.TrangThai;

            if (!string.IsNullOrEmpty(NewPassword))
            {
                tk.MatKhauHash = HashPassword(NewPassword);
            }

            await _context.SaveChangesAsync();
            TempData["msg"] = "Cập nhật tài khoản thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var tk = await _context.TaiKhoans.FindAsync(id);
            if (tk == null) return NotFound();

            var nv = await _context.NhanViens.FirstOrDefaultAsync(n => n.TaiKhoanId == id);
            if (nv != null)
            {
                TempData["error"] = $"Tài khoản đang gắn với nhân viên {nv.HoTen}. Hãy xóa nhân viên trước!";
                return RedirectToAction(nameof(Index));
            }

            _context.TaiKhoans.Remove(tk);
            await _context.SaveChangesAsync();

            TempData["msg"] = "Xóa tài khoản thành công!";
            return RedirectToAction(nameof(Index));
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
            }
        }
    }
}