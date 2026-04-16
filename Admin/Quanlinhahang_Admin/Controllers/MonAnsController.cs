using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Quanlinhahang.Data.Models;
using Quanlinhahang_Admin.Services;

namespace Quanlinhahang_Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MonAnsController : Controller
    {
        private readonly QuanLyNhaHangContext _context;
        private readonly IStorageService _storageService;

        public MonAnsController(QuanLyNhaHangContext context, IStorageService storageService)
        {
            _context = context;
            _storageService = storageService;
        }

        // ==========================================
        // 1. INDEX
        // ==========================================
        public async Task<IActionResult> Index()
        {
            // Tên DbSet đã đổi thành MonAns, Navigation property thành DanhMuc (hoặc DanhMucMon tùy bản EF Core sinh ra)
            var data = _context.MonAns.Include(m => m.DanhMuc);

            // Load danh sách danh mục để đổ vào ô tìm kiếm
            ViewBag.DanhMucList = await _context.DanhMucMons.ToListAsync();
            return View(await data.ToListAsync());
        }

        // ==========================================
        // 2. CREATE (GET) - Hiển thị form thêm
        // ==========================================
        public IActionResult Create()
        {
            LoadViewBag(); // 👇 Nạp dữ liệu cho Dropdown
            return View();
        }

        // ==========================================
        // 3. CREATE (POST) - Xử lý thêm
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MonAn monAn, IFormFile fHinhAnh)
        {
            ModelState.Remove("DanhMuc"); // Loại bỏ Navigation Property để pass Validation

            if (ModelState.IsValid)
            {
                if (fHinhAnh != null)
                {
                    monAn.HinhAnhUrl = await _storageService.SaveFileAsync(fHinhAnh);
                }

                if (string.IsNullOrEmpty(monAn.HinhAnhUrl))
                {
                    monAn.HinhAnhUrl = "default.jpg";
                }

                _context.Add(monAn);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            LoadViewBag(monAn.DanhMucId, monAn.TrangThai);
            return View(monAn);
        }

        // ==========================================
        // 4. EDIT (GET) - Hiển thị form sửa
        // ==========================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var monAn = await _context.MonAns
                .Include(m => m.DanhMuc)
                .FirstOrDefaultAsync(m => m.MonAnId == id);

            if (monAn == null) return NotFound();

            LoadViewBag(monAn.DanhMucId, monAn.TrangThai);
            return View(monAn);
        }

        // ==========================================
        // 5. EDIT (POST) - Xử lý sửa 
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MonAn monAn, IFormFile? fHinhAnh)
        {
            if (id != monAn.MonAnId) return NotFound();

            ModelState.Remove("DanhMuc");
            ModelState.Remove("HinhAnhUrl");
            ModelState.Remove("LoaiMon");
            // Đã xóa ModelState.Remove("Ngaytao") vì schema mới không có cột này trong MonAn

            if (ModelState.IsValid)
            {
                try
                {
                    var existingMon = await _context.MonAns.FindAsync(id);
                    if (existingMon == null) return NotFound();

                    existingMon.TenMon = monAn.TenMon;
                    existingMon.DanhMucId = monAn.DanhMucId;
                    existingMon.DonGia = monAn.DonGia;
                    existingMon.TrangThai = monAn.TrangThai;
                    existingMon.MoTa = monAn.MoTa;

                    if (fHinhAnh != null)
                    {
                        if (!string.IsNullOrEmpty(existingMon.HinhAnhUrl) && existingMon.HinhAnhUrl != "default.jpg")
                        {
                            await _storageService.DeleteFileAsync(existingMon.HinhAnhUrl);
                        }

                        existingMon.HinhAnhUrl = await _storageService.SaveFileAsync(fHinhAnh);
                    }

                    await _context.SaveChangesAsync();
                    TempData["msg"] = "Cập nhật thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi lưu: " + ex.Message);
                }
            }

            // Nếu thất bại, nạp lại Dropdown để hiện Form
            LoadViewBag(monAn.DanhMucId, monAn.TrangThai);
            return View(monAn);
        }

        // ==========================================
        // 6. DELETE
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mon = await _context.MonAns.FindAsync(id);
            if (mon == null) return NotFound();

            // Kiểm tra và xóa dữ liệu liên kết ở ChiTietHoaDon
            var ct = await _context.ChiTietHoaDons.Where(x => x.MonAnId == id).ToListAsync();
            if (ct.Any())
            {
                _context.ChiTietHoaDons.RemoveRange(ct);
            }

            if (!string.IsNullOrEmpty(mon.HinhAnhUrl) && mon.HinhAnhUrl != "default.jpg")
            {
                await _storageService.DeleteFileAsync(mon.HinhAnhUrl);
            }

            _context.MonAns.Remove(mon);
            await _context.SaveChangesAsync();
            return Ok(new { message = "success" });
        }

        private bool MonAnExists(int id) => _context.MonAns.Any(e => e.MonAnId == id);

        private void LoadViewBag(object selectedDanhmuc = null, object selectedTrangThai = null)
        {
            // Đã cập nhật thành DanhMucId và TenDanhMuc
            ViewData["DanhMucID"] = new SelectList(_context.DanhMucMons, "DanhMucId", "TenDanhMuc", selectedDanhmuc);

            var statusList = new List<string> { "Đang phục vụ", "Ngừng phục vụ", "Hết hàng" };
            ViewBag.TrangThaiList = new SelectList(statusList, selectedTrangThai);
        }
    }
}