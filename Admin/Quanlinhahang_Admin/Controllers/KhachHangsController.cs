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

        public async Task<IActionResult> Index(string? searchType, string? searchPhone, string? searchRank)
        {
            searchType = searchType ?? "all";

            var query = _context.Khachhangs
                .Include(k => k.Hangthanhvien)
                .AsQueryable(); ;

            ViewBag.SearchType = searchType;
            ViewBag.SearchPhone = searchPhone ?? "";
            ViewBag.SearchRank = searchRank ?? "";

            if (searchType == "all")
                return View(await query.ToListAsync());

            if (searchType == "phone" && !string.IsNullOrWhiteSpace(searchPhone))
            {
                query = query.Where(k => k.Sodienthoai.Contains(searchPhone));
            }
            else if (searchType == "rank" && !string.IsNullOrWhiteSpace(searchRank))
            {
                query = query.Where(k => k.Hangthanhvien != null && k.Hangthanhvien.Tenhang == searchRank);
            }

            var list = await query.OrderByDescending(k => k.Khachhangid).ToListAsync();

            if (!list.Any())
                TempData["msg"] = "Không tìm thấy khách hàng nào!";

            return View(list); 
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var kh = await _context.Khachhangs.FindAsync(id);
            if (kh == null)
                return NotFound();

            try
            {
                var lichSuDatBan = await _context.Datbans.Where(d => d.Khachhangid == id).ToListAsync();
                foreach (var item in lichSuDatBan)
                {
                    item.Khachhangid = null;
                }

                _context.Khachhangs.Remove(kh);
                await _context.SaveChangesAsync();

                return Ok(); // Trả về 200 OK cho AJAX
            }
            catch (Exception ex)
            {
                return BadRequest("Không thể xóa khách hàng: " + ex.Message);
            }
        }
    }
}
