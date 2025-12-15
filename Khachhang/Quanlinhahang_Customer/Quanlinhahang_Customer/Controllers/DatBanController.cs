using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quanlinhahang.Data.Models;
using Quanlinhahang_Customer.Models.ViewModels;
using System.Globalization;

namespace Quanlinhahang_Customer.Controllers
{
    public class DatBanController : Controller
    {
        private readonly QuanLyNhaHangContext _context;

        public DatBanController(QuanLyNhaHangContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new DatBanViewModel());
        }

        public class BookingItem
        {
            public int? id { get; set; }
            public string? name { get; set; }
            public decimal? price { get; set; }
            public int? qty { get; set; }
        }

        public class BookingRequest
        {
            public string? username { get; set; }
            public string? bookingDate { get; set; }
            public string? timeSlot { get; set; }
            public int? guestCount { get; set; }
            public int? BanPhongId { get; set; }
            public string? note { get; set; }
            public List<BookingItem>? items { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> GetTableStatus(string date, string timeSlot)
        {
            if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime searchDate))
                return BadRequest(new { message = "Ngày không hợp lệ." });

            int khungGioId = await ResolveKhungGioId(timeSlot);
            if (khungGioId == 0) return BadRequest(new { message = "Vui lòng chọn khung giờ." });

            var allTables = await _context.Banphongs.Include(b => b.Loaibanphong).OrderBy(b => b.Banphongid).ToListAsync();

            var bookedTableIds = await _context.Datbans
                .Where(d => d.Ngayden.Date == searchDate.Date
                         && d.Khunggioid == khungGioId
                         && d.Trangthai != "Đã hủy"
                         && d.Banphongid != null)
                .Select(d => (int?)d.Banphongid)
                .ToListAsync();

            var result = allTables.Select(b => new
            {
                id = b.Banphongid,
                name = b.Tenbanphong,
                capacity = b.Succhua,
                status = (bookedTableIds.Contains(b.Banphongid) || b.Trangthai) ? 1 : 0,
                typeName = b.Loaibanphong.Tenloai
            });

            return Json(new { success = true, data = result });
        }

        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] BookingRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.username))
                return Unauthorized(new { success = false, message = "Bạn cần đăng nhập để đặt bàn." });

            var khachHang = await _context.Khachhangs.Include(k => k.Taikhoan)
                .FirstOrDefaultAsync(k => k.Taikhoan != null && k.Taikhoan.Tendangnhap == req.username);

            if (khachHang == null) return NotFound(new { success = false, message = "Không tìm thấy khách hàng." });

            var items = req.items ?? new List<BookingItem>();
            if (items.Count == 0) return Json(new { success = false, message = "Giỏ hàng rỗng." });

            if (!DateTime.TryParseExact(req.bookingDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime bookingDate))
                return Json(new { success = false, message = "Ngày không hợp lệ" });

            int khungGioId = await ResolveKhungGioId(req.timeSlot);
            if (khungGioId == 0) return Json(new { success = false, message = "Khung giờ không hợp lệ" });

            int? banPhongId = req.BanPhongId;
            if (banPhongId.HasValue && banPhongId > 0)
            {
                var banDaChon = await _context.Banphongs.FindAsync(banPhongId.Value);
                if (banDaChon == null || banDaChon.Trangthai) return Json(new { success = false, message = "Bàn không khả dụng." });

                bool isBooked = await _context.Datbans.AnyAsync(d => d.Ngayden.Date == bookingDate.Date && d.Khunggioid == khungGioId && d.Banphongid == banPhongId.Value && d.Trangthai != "Đã hủy");
                if (isBooked) return Json(new { success = false, message = "Bàn đã có người đặt." });
            }

            decimal tongTienTinhToan = items.Sum(i => (i.price ?? 0) * (i.qty ?? 1));

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var datBan = new Datban
                    {
                        Khachhangid = khachHang.Khachhangid,
                        Banphongid = banPhongId,
                        Khunggioid = khungGioId,
                        Ngayden = bookingDate,
                        Songuoi = req.guestCount ?? 1,
                        Tongtiendukien = (long)tongTienTinhToan, // Fix type long
                        Yeucaudacbiet = req.note,
                        Trangthai = "Chờ xác nhận",
                        Ngaytao = DateTime.Now
                    };
                    _context.Datbans.Add(datBan);
                    await _context.SaveChangesAsync();

                    var hoaDon = new Hoadon
                    {
                        Datbanid = datBan.Datbanid,
                        Banphongid = banPhongId,
                        Ngaylap = DateTime.Now,
                        Tongtien = (long)tongTienTinhToan, // Fix type long
                        Giamgia = 0,
                        Diemcong = 0,
                        Diemsudung = 0,
                        Trangthaiid = 1,
                        Vat = 10
                    };
                    _context.Hoadons.Add(hoaDon);
                    await _context.SaveChangesAsync();

                    foreach (var item in items)
                    {
                        if (item.id.HasValue && item.id.Value > 0)
                        {
                            var chiTiet = new Chitiethoadon
                            {
                                Hoadonid = hoaDon.Hoadonid,
                                Monanid = (item.id ?? 0),
                                Soluong = item.qty ?? 1,
                                Dongia = (long)(item.price ?? 0), // Fix type long
                                Thanhtien = (long)((item.price ?? 0) * (item.qty ?? 1)) // Fix type long
                            };
                            _context.Chitiethoadons.Add(chiTiet);
                        }
                    }
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return Json(new { success = true, message = "Đặt bàn thành công!", datBanId = datBan.Datbanid });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { success = false, message = "Lỗi: " + ex.Message });
                }
            }
        }

        private async Task<int> ResolveKhungGioId(string? timeSlot)
        {
            if (string.IsNullOrWhiteSpace(timeSlot)) return 0;
            string key = timeSlot.Trim().ToLower();
            if (key.Contains("trua") || key.Contains("trưa")) key = "Trưa";
            else if (key.Contains("toi") || key.Contains("tối")) key = "Tối";
            else return 0;

            var khungGio = await _context.Khunggios.FirstOrDefaultAsync(k => k.Tenkhunggio == key);
            return khungGio != null ? (int)khungGio.Khunggioid : 0;
        }
    }
}