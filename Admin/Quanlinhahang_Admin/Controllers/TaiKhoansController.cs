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
            var data = await _context.Taikhoans
                .Where(t => t.Vaitro != VaiTroHeThong.Customer)
                .OrderBy(t => t.Vaitro)
                .ToListAsync();

            return View(data);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Taikhoan model, string MatKhau)
        {
            if (await _context.Taikhoans.AnyAsync(t => t.Tendangnhap == model.Tendangnhap))
            {
                ModelState.AddModelError("Tendangnhap", "Tên đăng nhập đã tồn tại");
                return View(model);
            }

            model.Matkhauhash = HashPassword(MatKhau);

            model.Trangthai = "Hoạt động";

            _context.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var tk = await _context.Taikhoans.FindAsync(id);
            if (tk == null) return NotFound();
            return View(tk);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Taikhoan model, string? NewPassword)
        {
            if (id != model.Taikhoanid) return NotFound();

            var tk = await _context.Taikhoans.FindAsync(id);
            if (tk == null) return NotFound();

            tk.Email = model.Email;
            tk.Vaitro = model.Vaitro; 
            tk.Trangthai = model.Trangthai;

            if (!string.IsNullOrEmpty(NewPassword))
            {
                tk.Matkhauhash = HashPassword(NewPassword);
            }

            await _context.SaveChangesAsync();
            TempData["msg"] = "Cập nhật tài khoản thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var tk = await _context.Taikhoans.FindAsync(id);
            if (tk == null) return NotFound();

            var nv = await _context.Nhanviens.FirstOrDefaultAsync(n => n.Taikhoanid == id);
            if (nv != null)
            {

                TempData["error"] = $"Tài khoản đang gắn với nhân viên {nv.Hoten}. Hãy xóa nhân viên trước!";
                return RedirectToAction(nameof(Index));

            }

            _context.Taikhoans.Remove(tk);
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
