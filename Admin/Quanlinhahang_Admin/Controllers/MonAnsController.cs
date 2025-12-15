using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Quanlinhahang.Data.Models;
using Quanlinhahang_Admin.Services;

namespace Quanlinhahang_Admin.Controllers
{
    public class MonAnsController : Controller
    {
        private readonly QuanLyNhaHangContext _context;
        private readonly IStorageService _storageService;

        public MonAnsController(QuanLyNhaHangContext context, IStorageService storageService)
        {
            _context = context;
            _storageService = storageService;
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Monan monAn, IFormFile fHinhAnh)
        {
            if (ModelState.IsValid)
            {
                if (fHinhAnh != null)
                {
                    // Lưu vào SharedImages thông qua Service
                    monAn.Hinhanhurl = await _storageService.SaveFileAsync(fHinhAnh);
                }

                _context.Add(monAn);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            // Load lại ViewBag nếu lỗi
            ViewBag.DanhMucID = new SelectList(_context.Danhmucmons, "Danhmucid", "Tendanhmuc", monAn.Danhmucid);
            return View(monAn);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Monan monAn, IFormFile? HinhAnh)
        {
            if (id != monAn.Monanid) return NotFound();

            // Lưu ý: Validate ModelState ở đây nếu cần

            var existing = await _context.Monans.FindAsync(id);
            if (existing == null) return NotFound();

            // Cập nhật thông tin
            existing.Tenmon = monAn.Tenmon;
            existing.Danhmucid = monAn.Danhmucid;
            existing.Dongia = monAn.Dongia;
            existing.Trangthai = monAn.Trangthai;
            existing.Mota = monAn.Mota;
            if (HinhAnh != null)
            {
                // 1. Xóa ảnh cũ (nếu có) khỏi SharedImages
                if (!string.IsNullOrEmpty(existing.Hinhanhurl))
                {
                    await _storageService.DeleteFileAsync(existing.Hinhanhurl);
                }

                // 2. Lưu ảnh mới vào SharedImages
                existing.Hinhanhurl = await _storageService.SaveFileAsync(HinhAnh);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Monans.Any(e => e.Monanid == id)) return NotFound();
                else throw;
            }

            TempData["msg"] = "Cập nhật thành công!";
            return RedirectToAction(nameof(Index));
        }

        // =============================
        // DELETE (AJAX) - ĐÃ SỬA
        // =============================
        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mon = await _context.Monans.FindAsync(id);
            if (mon == null) return NotFound();

            // Xử lý xóa ràng buộc con (Chi tiết hóa đơn)
            var ct = await _context.Chitiethoadons.Where(x => x.Monanid == id).ToListAsync();
            if (ct.Any()) _context.Chitiethoadons.RemoveRange(ct);

            // Xóa ảnh vật lý trong SharedImages
            if (!string.IsNullOrEmpty(mon.Hinhanhurl))
            {
                await _storageService.DeleteFileAsync(mon.Hinhanhurl);
            }

            _context.Monans.Remove(mon);
            await _context.SaveChangesAsync();

            return Ok(new { message = "success" });
        }
    }
}