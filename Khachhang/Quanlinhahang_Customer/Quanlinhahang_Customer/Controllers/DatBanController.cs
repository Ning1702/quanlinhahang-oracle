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

            if (searchDate.Date < DateTime.Today)
            {
                return BadRequest(new { message = "Không thể xem dữ liệu của ngày trong quá khứ." });
            }

            var dateOnlySearch = DateOnly.FromDateTime(searchDate);

            int khungGioId = await ResolveKhungGioId(timeSlot);
            if (khungGioId == 0) return BadRequest(new { message = "Vui lòng chọn khung giờ." });

            var allTables = await _context.BanPhongs
                .Include(b => b.LoaiBanPhong)
                .OrderBy(b => b.BanPhongId)
                .ToListAsync();

            var bookedTableIds = await _context.DatBans
                .Where(d => d.NgayDen == dateOnlySearch
                         && d.KhungGioId == khungGioId
                         && d.TrangThaiId != 5
                         && d.BanPhongId != null)
                .Select(d => d.BanPhongId)
                .ToListAsync();

            var result = allTables.Select(b => new
            {
                id = b.BanPhongId,
                name = b.TenBanPhong,
                capacity = b.SucChua,
                status = (bookedTableIds.Contains(b.BanPhongId) || b.TrangThaiId != 0) ? 1 : 0,
                typeName = b.LoaiBanPhong?.TenLoai ?? "Khác"
            });

            return Json(new { success = true, data = result });
        }

        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] BookingRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.username))
                return Unauthorized(new { success = false, message = "Bạn cần đăng nhập để đặt bàn." });

            var khachHang = await _context.KhachHangs
                .Include(k => k.TaiKhoan)
                .FirstOrDefaultAsync(k => k.TaiKhoan != null && k.TaiKhoan.TenDangNhap == req.username);

            if (khachHang == null)
                return NotFound(new { success = false, message = "Không tìm thấy khách hàng." });

            var items = req.items ?? new List<BookingItem>();
            if (items.Count == 0)
                return Json(new { success = false, message = "Giỏ hàng rỗng." });

            if (!DateTime.TryParseExact(req.bookingDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime bookingDateTime))
                return Json(new { success = false, message = "Ngày không hợp lệ." });

            var today = DateTime.Today;
            var nowLocal = DateTime.Now;
            var bookingDate = DateOnly.FromDateTime(bookingDateTime);

            if (bookingDateTime.Date < today)
            {
                return Json(new { success = false, message = "Không thể đặt bàn cho ngày trong quá khứ." });
            }

            int khungGioId = await ResolveKhungGioId(req.timeSlot);
            if (khungGioId == 0)
                return Json(new { success = false, message = "Khung giờ không hợp lệ." });

            var khungGio = await _context.KhungGios
                .FirstOrDefaultAsync(k => k.KhungGioId == khungGioId);

            if (khungGio == null)
                return Json(new { success = false, message = "Không tìm thấy thông tin khung giờ." });

            // Nếu đặt trong ngày hôm nay thì khung giờ phải chưa bắt đầu
            if (bookingDateTime.Date == today && khungGio.GioBatDau.ToTimeSpan() <= nowLocal.TimeOfDay)
            {
                return Json(new
                {
                    success = false,
                    message = $"Khung giờ {khungGio.TenKhungGio} ({khungGio.GioBatDau:HH\\:mm} - {khungGio.GioKetThuc:HH\\:mm}) đã qua, vui lòng chọn khung giờ khác hoặc ngày khác."
                });
            }

            int? banPhongId = req.BanPhongId;
            if (banPhongId.HasValue && banPhongId > 0)
            {
                var banDaChon = await _context.BanPhongs.FindAsync(banPhongId.Value);
                if (banDaChon == null || banDaChon.TrangThaiId != 0)
                    return Json(new { success = false, message = "Bàn không khả dụng." });

                bool isBooked = await _context.DatBans.AnyAsync(d =>
                    d.NgayDen == bookingDate &&
                    d.KhungGioId == khungGioId &&
                    d.BanPhongId == banPhongId.Value &&
                    d.TrangThaiId != 5);

                if (isBooked)
                    return Json(new { success = false, message = "Bàn đã có người đặt." });
            }

            decimal tongTienTinhToan = items.Sum(i => (i.price ?? 0m) * (i.qty ?? 1));

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var datBan = new DatBan
                    {
                        KhachHangId = khachHang.KhachHangId,
                        BanPhongId = banPhongId,
                        KhungGioId = khungGioId,
                        NgayDen = bookingDate,
                        SoNguoi = req.guestCount ?? 1,
                        YeuCauDacBiet = req.note,
                        TrangThaiId = 1,
                        ThoiGianTaoDon = DateTime.UtcNow
                    };

                    _context.DatBans.Add(datBan);
                    await _context.SaveChangesAsync();

                    var hoaDon = new HoaDon
                    {
                        DatBanId = datBan.DatBanId,
                        NgayLap = DateTime.UtcNow,
                        TongTien = tongTienTinhToan,
                        TrangThaiId = 1,
                        Vatpercent = 10m,
                        TaiKhoanId = khachHang.TaiKhoanId
                    };

                    _context.HoaDons.Add(hoaDon);
                    await _context.SaveChangesAsync();

                    foreach (var item in items)
                    {
                        if (item.id.HasValue && item.id.Value > 0)
                        {
                            _context.ChiTietHoaDons.Add(new ChiTietHoaDon
                            {
                                HoaDonId = hoaDon.HoaDonId,
                                MonAnId = item.id.Value,
                                SoLuong = item.qty ?? 1,
                                DonGia = item.price ?? 0m
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    try
                    {
                        var handler = new HttpClientHandler
                        {
                            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                        };

                        using (var client = new HttpClient(handler))
                        {
                            string ten = khachHang.HoTen ?? "Khách hàng";
                            string sdt = khachHang.SoDienThoai ?? "0000000000";

                            string staffApiUrl =
                                $"https://localhost:7163/api/NotifyNewBooking?tenKhach={Uri.EscapeDataString(ten)}&soDienThoai={Uri.EscapeDataString(sdt)}";

                            await client.PostAsync(staffApiUrl, null);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("LỖI GỬI TÍN HIỆU SIGNALR: " + ex.Message);
                    }

                    return Json(new
                    {
                        success = true,
                        message = "Đặt bàn thành công!",
                        datBanId = datBan.DatBanId
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "LỖI DB: " + (ex.InnerException?.Message ?? ex.Message)
                    });
                }
            }
        }

        private async Task<int> ResolveKhungGioId(string? timeSlot)
        {
            if (string.IsNullOrWhiteSpace(timeSlot)) return 0;

            string key = timeSlot.Trim();

            if (key.Contains("trua", StringComparison.OrdinalIgnoreCase) || key.Contains("trưa", StringComparison.OrdinalIgnoreCase))
                key = "Trưa";
            else if (key.Contains("toi", StringComparison.OrdinalIgnoreCase) || key.Contains("tối", StringComparison.OrdinalIgnoreCase))
                key = "Tối";
            else
                return 0;

            var khungGio = await _context.KhungGios.FirstOrDefaultAsync(k => k.TenKhungGio == key);
            return khungGio != null ? khungGio.KhungGioId : 0;
        }
    }
}