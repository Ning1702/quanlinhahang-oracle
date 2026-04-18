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
            // Load riêng từng bảng để tránh lỗi join/navigation khi mapping PostgreSQL lệch
            var monAns = await _context.MonAns
                .AsNoTracking()
                .OrderByDescending(m => m.MonAnId)
                .ToListAsync();

            var danhMucs = await _context.DanhMucMons
                .AsNoTracking()
                .ToListAsync();

            var danhMucMap = danhMucs.ToDictionary(d => d.DanhMucId, d => d);

            foreach (var mon in monAns)
            {
                if (danhMucMap.TryGetValue(mon.DanhMucId ?? 0, out var dm))
                {
                    mon.DanhMuc = dm;
                }
            }

            ViewBag.DanhMucList = danhMucs;
            return View(monAns);
        }

        // ==========================================
        // 2. CREATE (GET)
        // ==========================================
        public IActionResult Create()
        {
            LoadViewBag();
            return View();
        }

        // ==========================================
        // 3. CREATE (POST)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MonAn monAn, IFormFile? fHinhAnh)
        {
            ModelState.Remove("DanhMuc");

            if (ModelState.IsValid)
            {
                if (fHinhAnh != null)
                {
                    monAn.HinhAnhUrl = await _storageService.SaveFileAsync(fHinhAnh);
                }

                if (string.IsNullOrWhiteSpace(monAn.HinhAnhUrl))
                {
                    monAn.HinhAnhUrl = "default.jpg";
                }

                _context.MonAns.Add(monAn);
                await _context.SaveChangesAsync();

                TempData["msg"] = "Thêm món ăn thành công!";
                return RedirectToAction(nameof(Index));
            }

            LoadViewBag(monAn.DanhMucId, monAn.TrangThai);
            return View(monAn);
        }

        // ==========================================
        // 4. EDIT (GET)
        // ==========================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var monAn = await _context.MonAns
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MonAnId == id);

            if (monAn == null) return NotFound();

            LoadViewBag(monAn.DanhMucId, monAn.TrangThai);
            return View(monAn);
        }

        // ==========================================
        // 5. EDIT (POST)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MonAn monAn, IFormFile? fHinhAnh)
        {
            if (id != monAn.MonAnId) return NotFound();

            ModelState.Remove("DanhMuc");
            ModelState.Remove("HinhAnhUrl");
            ModelState.Remove("LoaiMon");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingMon = await _context.MonAns.FirstOrDefaultAsync(x => x.MonAnId == id);
                    if (existingMon == null) return NotFound();

                    existingMon.TenMon = monAn.TenMon;
                    existingMon.DanhMucId = monAn.DanhMucId;
                    existingMon.DonGia = monAn.DonGia;
                    existingMon.TrangThai = monAn.TrangThai;
                    existingMon.MoTa = monAn.MoTa;

                    if (fHinhAnh != null)
                    {
                        if (!string.IsNullOrWhiteSpace(existingMon.HinhAnhUrl) && existingMon.HinhAnhUrl != "default.jpg")
                        {
                            await _storageService.DeleteFileAsync(existingMon.HinhAnhUrl);
                        }

                        existingMon.HinhAnhUrl = await _storageService.SaveFileAsync(fHinhAnh);
                    }

                    if (string.IsNullOrWhiteSpace(existingMon.HinhAnhUrl))
                    {
                        existingMon.HinhAnhUrl = "default.jpg";
                    }

                    await _context.SaveChangesAsync();
                    TempData["msg"] = "Cập nhật món ăn thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi lưu dữ liệu: " + ex.Message);
                }
            }

            LoadViewBag(monAn.DanhMucId, monAn.TrangThai);
            return View(monAn);
        }

        // ==========================================
        // 6. DELETE
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mon = await _context.MonAns.FirstOrDefaultAsync(x => x.MonAnId == id);
            if (mon == null) return NotFound();

            var ct = await _context.ChiTietHoaDons
                .Where(x => x.MonAnId == id)
                .ToListAsync();

            if (ct.Any())
            {
                _context.ChiTietHoaDons.RemoveRange(ct);
            }

            if (!string.IsNullOrWhiteSpace(mon.HinhAnhUrl) && mon.HinhAnhUrl != "default.jpg")
            {
                await _storageService.DeleteFileAsync(mon.HinhAnhUrl);
            }

            _context.MonAns.Remove(mon);
            await _context.SaveChangesAsync();

            return Ok(new { message = "success" });
        }

        private bool MonAnExists(int id) => _context.MonAns.Any(e => e.MonAnId == id);

        private void LoadViewBag(object? selectedDanhmuc = null, object? selectedTrangThai = null)
        {
            ViewData["DanhMucID"] = new SelectList(
                _context.DanhMucMons.AsNoTracking().ToList(),
                "DanhMucId",
                "TenDanhMuc",
                selectedDanhmuc
            );

            var statusList = new List<string> { "Đang phục vụ", "Ngừng phục vụ", "Hết hàng" };
            ViewBag.TrangThaiList = new SelectList(statusList, selectedTrangThai);
        }
    }
}