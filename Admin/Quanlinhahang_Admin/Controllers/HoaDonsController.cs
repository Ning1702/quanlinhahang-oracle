using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Quanlinhahang.Data.Models;

namespace Quanlinhahang_Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class HoaDonsController : Controller
    {
        private readonly QuanLyNhaHangContext _ctx;
        public HoaDonsController(QuanLyNhaHangContext ctx) => _ctx = ctx;

        public async Task<IActionResult> Index(string? status)
        {
            var list = _ctx.HoaDons
                .Include(h => h.DatBan).ThenInclude(db => db.KhachHang)
                .Include(h => h.TrangThai)
                .OrderByDescending(h => h.NgayLap)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                if (status == "paid")
                    list = list.Where(h => h.TrangThaiId == 4);
                else if (status == "unpaid")
                    list = list.Where(h => h.TrangThaiId == 1);
            }

            ViewBag.CurrentStatus = status;
            return View(await list.ToListAsync());
        }

        public async Task<IActionResult> Edit(int id)
        {
            var hd = await _ctx.HoaDons.FindAsync(id);
            if (hd == null) return NotFound();
            return View(hd);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, HoaDon model)
        {
            if (id != model.HoaDonId) return BadRequest();

            var hd = await _ctx.HoaDons.FindAsync(id);
            if (hd == null) return NotFound();

            // Sửa lỗi: Cột VAT trong SQL mới là VATPercent -> EF map thành Vatpercent
            hd.Vatpercent = model.Vatpercent;
            await _ctx.SaveChangesAsync();

            TempData["msg"] = "Cập nhật hóa đơn thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPaid(int id)
        {
            var hd = await _ctx.HoaDons.FindAsync(id);
            if (hd == null) return NotFound();

            hd.TrangThaiId = 4;
            await _ctx.SaveChangesAsync();

            TempData["msg"] = $"Hóa đơn #{id} đã được đánh dấu ĐÃ THANH TOÁN.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkUnpaid(int id)
        {
            var hd = await _ctx.HoaDons.FindAsync(id);
            if (hd == null) return NotFound();

            hd.TrangThaiId = 1;
            await _ctx.SaveChangesAsync();

            TempData["msg"] = $"Hóa đơn #{id} đã được đánh dấu CHƯA THANH TOÁN.";
            return RedirectToAction(nameof(Index));
        }
    }
}