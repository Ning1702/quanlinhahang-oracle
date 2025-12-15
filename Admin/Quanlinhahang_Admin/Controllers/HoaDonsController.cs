using System.Linq;
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
            var list = _ctx.Hoadons
                .Include(h => h.Datban).ThenInclude(db => db.Khachhang)
                .Include(h => h.Trangthai) 
                .OrderByDescending(h => h.Ngaylap)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                if (status == "paid")
                    list = list.Where(h => h.Trangthaiid == 4);
                else if (status == "unpaid")
                    list = list.Where(h => h.Trangthaiid == 1);
            }

            ViewBag.CurrentStatus = status;
            return View(await list.ToListAsync());
        }

        public async Task<IActionResult> Edit(long id)
        {
            var hd = await _ctx.Hoadons.FindAsync(id);
            if (hd == null) return NotFound();
            return View(hd);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, Hoadon model)
        {
            if (id != model.Hoadonid) return BadRequest();
            var hd = await _ctx.Hoadons.FindAsync(id);
            if (hd == null) return NotFound();

            hd.Vat = model.Vat;
            //hd.LoaiDichVu = model.LoaiDichVu;
            await _ctx.SaveChangesAsync();

            TempData["msg"] = "Cập nhật hóa đơn thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPaid(int id)
        {
            var hd = await _ctx.Hoadons.FindAsync(id);
            if (hd == null) return NotFound();


            hd.Trangthaiid = 4;
            await _ctx.SaveChangesAsync();

            TempData["msg"] = $"Hóa đơn #{id} đã được đánh dấu ĐÃ THANH TOÁN.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkUnpaid(int id)
        {
            var hd = await _ctx.Hoadons.FindAsync(id);
            if (hd == null) return NotFound();

            hd.Trangthaiid = 1;
            await _ctx.SaveChangesAsync();

            TempData["msg"] = $"Hóa đơn #{id} đã được đánh dấu CHƯA THANH TOÁN.";
            return RedirectToAction(nameof(Index));
        }
    }
}
