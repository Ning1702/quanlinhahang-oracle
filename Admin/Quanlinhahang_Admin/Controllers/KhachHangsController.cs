using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quanlinhahang.Data.Models;
using System.Linq;

namespace Quanlinhahang_Admin.Controllers
{
    public class KhachHangsController : Controller
    {
        private readonly QuanLyNhaHangContext _context;

        public KhachHangsController(QuanLyNhaHangContext context)
        {
            _context = context;
        }

        // ============================================================
        // 1. DANH SÁCH KHÁCH HÀNG & TÌM KIẾM
        // ============================================================
        public async Task<IActionResult> Index(string? searchType, string? searchPhone, string? searchRank)
        {
            searchType = searchType ?? "all";

            // Cập nhật PascalCase: KhachHangs, HangThanhVien
            var query = _context.KhachHangs
                .Include(k => k.HangThanhVien)
                .AsQueryable();

            ViewBag.SearchType = searchType;
            ViewBag.SearchPhone = searchPhone ?? "";
            ViewBag.SearchRank = searchRank ?? "";

            if (searchType == "phone" && !string.IsNullOrWhiteSpace(searchPhone))
            {
                // Cập nhật PascalCase: SoDienThoai
                query = query.Where(k => k.SoDienThoai.Contains(searchPhone));
            }
            else if (searchType == "rank" && !string.IsNullOrWhiteSpace(searchRank))
            {
                // Cập nhật PascalCase: HangThanhVien, TenHang
                query = query.Where(k => k.HangThanhVien != null && k.HangThanhVien.TenHang == searchRank);
            }

            // Cập nhật PascalCase: KhachHangId
            var list = await query.OrderByDescending(k => k.KhachHangId).ToListAsync();

            if (!list.Any() && searchType != "all")
            {
                TempData["msg"] = "Không tìm thấy khách hàng nào khớp với điều kiện tìm kiếm!";
            }

            return View(list);
        }

        // ============================================================
        // 2. XÁC NHẬN XÓA (AJAX)
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Cập nhật PascalCase: KhachHangs
            var kh = await _context.KhachHangs.FindAsync(id);
            if (kh == null)
                return NotFound();

            try
            {
                // Xử lý ràng buộc dữ liệu: Gán null cho lịch sử đặt bàn của khách này trước khi xóa
                // Cập nhật PascalCase: DatBans, KhachHangId
                var lichSuDatBan = await _context.DatBans.Where(d => d.KhachHangId == id).ToListAsync();
                foreach (var item in lichSuDatBan)
                {
                    item.KhachHangId = null;
                }

                _context.KhachHangs.Remove(kh);
                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                // Trả về lỗi nếu có ràng buộc dữ liệu khác (ví dụ: Hóa đơn)
                return BadRequest("Không thể xóa khách hàng do có dữ liệu liên quan: " + ex.Message);
            }
        }
    }
}