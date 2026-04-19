using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Quanlinhahang.Data.Models;
using Quanlinhahang_Staff.Hubs;
using Quanlinhahang_Staff.Models.ViewModels;
using System.Text.Json;

namespace Quanlinhahang_Staff.Controllers
{
    public class InvoicesController : BaseController
    {
        private readonly QuanLyNhaHangContext _db;
        private readonly IHubContext<NotificationHub> _hubContext;

        public InvoicesController(QuanLyNhaHangContext db, IHubContext<NotificationHub> hubContext)
        {
            _db = db;
            _hubContext = hubContext;
        }

        [HttpPost]
        [Route("api/NotifyNewBooking")]
        public async Task<IActionResult> NotifyNewBooking(string tenKhach, string soDienThoai)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveNewBooking", tenKhach, soDienThoai);
            return Ok(new { success = true });
        }

        // ==============================
        // LIVE SEARCH KHÁCH HÀNG
        // ==============================
        [HttpGet]
        public async Task<IActionResult> SearchCustomer(string keyword)
        {
            keyword = (keyword ?? "").Trim();

            if (string.IsNullOrWhiteSpace(keyword))
                return Json(new List<object>());

            var data = await _db.KhachHangs
                .Where(k =>
                    (k.HoTen != null && k.HoTen.ToLower().Contains(keyword.ToLower())) ||
                    (k.SoDienThoai != null && k.SoDienThoai.Contains(keyword)))
                .OrderBy(k => k.HoTen)
                .Select(k => new
                {
                    khachHangId = k.KhachHangId,
                    hoTen = k.HoTen,
                    soDienThoai = k.SoDienThoai
                })
                .Take(10)
                .ToListAsync();

            return Json(data);
        }

        // ==============================
        // LIST HÓA ĐƠN
        // ==============================
        public async Task<IActionResult> Index([FromQuery] InvoiceFilterVM f, [FromQuery] int status = 0)
        {
            if (!IsLoggedIn) return RequireLogin();

            ViewBag.Status = status;
            ViewBag.Filter = f;

            var query = _db.HoaDons
                .Include(h => h.TrangThai)
                .Include(h => h.DatBan).ThenInclude(db => db.KhachHang)
                .Include(h => h.DatBan).ThenInclude(db => db.BanPhong).ThenInclude(bp => bp.LoaiBanPhong)
                .Include(h => h.DatBan).ThenInclude(db => db.KhungGio)
                .Include(h => h.ChiTietHoaDons)
                .AsQueryable();

            if (status > 0)
                query = query.Where(h => h.TrangThaiId == status);

            if (!string.IsNullOrWhiteSpace(f.Search))
            {
                string s = f.Search.Trim().ToLower();
                query = query.Where(h =>
                    (h.DatBan != null && h.DatBan.KhachHang != null && h.DatBan.KhachHang.HoTen != null && h.DatBan.KhachHang.HoTen.ToLower().Contains(s)) ||
                    (h.DatBan != null && h.DatBan.KhachHang != null && (h.DatBan.KhachHang.SoDienThoai ?? "").Contains(s)));
            }

            if (f.From.HasValue)
            {
                var fromDate = f.From.Value.Date;
                query = query.Where(h => h.NgayLap.HasValue && h.NgayLap.Value.Date >= fromDate);
            }

            if (f.To.HasValue)
            {
                var toDate = f.To.Value.Date;
                query = query.Where(h => h.NgayLap.HasValue && h.NgayLap.Value.Date <= toDate);
            }

            var rawList = await query
                .OrderByDescending(h => h.HoaDonId)
                .ToListAsync();

            var list = rawList.Select(h =>
            {
                decimal subTotal = h.ChiTietHoaDons?.Sum(ct => ct.ThanhTien ?? 0m) ?? 0m;
                decimal vatPercent = h.Vatpercent ?? 10m;
                decimal thanhTien = subTotal * (1 + vatPercent / 100m);

                return new InvoiceRowVM
                {
                    HoaDonID = h.HoaDonId,
                    NgayLap = h.NgayLap ?? DateTime.UtcNow,
                    NgayDen = (h.DatBan != null && h.DatBan.NgayDen.HasValue)
                        ? h.DatBan.NgayDen.Value.ToDateTime(TimeOnly.MinValue)
                        : DateTime.MinValue,
                    KhungGio = h.DatBan?.KhungGio?.TenKhungGio ?? "---",
                    KhachHang = h.DatBan?.KhachHang?.HoTen ?? "Khách vãng lai",
                    SoDienThoai = h.DatBan?.KhachHang?.SoDienThoai ?? "",
                    BanPhong = h.DatBan?.BanPhong?.TenBanPhong ?? "",
                    LoaiBanPhong = h.DatBan?.BanPhong?.LoaiBanPhong?.TenLoai ?? "",
                    ThanhTien = thanhTien,
                    TrangThaiID = h.TrangThaiId ?? 0,
                    TrangThaiTen = h.TrangThai?.TenTrangThai ?? "Không xác định"
                };
            }).ToList();

            return View(list);
        }

        // ==============================
        // CÁC HÀM XỬ LÝ TRẠNG THÁI
        // ==============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmInvoice(int id, int status = 0)
        {
            var hd = await _db.HoaDons.FindAsync(id);
            if (hd == null) return NotFound();

            if (hd.TrangThaiId == 1)
            {
                hd.TrangThaiId = 2;

                int? staffId = HttpContext.Session.GetInt32("AccountId");
                if (staffId.HasValue)
                    hd.TaiKhoanId = staffId.Value;

                await _db.SaveChangesAsync();

                TempData["msg"] = $"✅ Đã xác nhận hóa đơn #{id}.";
                return RedirectToAction(nameof(Index), new { status, highlight = id });
            }

            TempData["msg"] = "⚠️ Không thể xác nhận hóa đơn này.";
            return RedirectToAction(nameof(Index), new { status });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartServing(int id, int status = 0)
        {
            var hd = await _db.HoaDons.FindAsync(id);
            if (hd == null) return NotFound();

            if (hd.TrangThaiId == 2)
            {
                hd.TrangThaiId = 3;
                await _db.SaveChangesAsync();
                TempData["msg"] = $"✅ Đã chuyển hóa đơn #{id} sang trạng thái đang phục vụ.";
                return RedirectToAction(nameof(Index), new { status, highlight = id });
            }

            TempData["msg"] = "⚠️ Hóa đơn chưa được xác nhận hoặc trạng thái không hợp lệ.";
            return RedirectToAction(nameof(Index), new { status });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThanhToan(int id, int status = 0)
        {
            var hd = await _db.HoaDons
                .Include(h => h.ChiTietHoaDons)
                .FirstOrDefaultAsync(h => h.HoaDonId == id);

            if (hd == null) return NotFound();

            if (hd.TrangThaiId == 3)
            {
                await UpdateTongTienAsync(hd);
                hd.TrangThaiId = 4;
                await _db.SaveChangesAsync();

                TempData["msg"] = $"💰 Đã thanh toán thành công hóa đơn #{id}.";
                return RedirectToAction(nameof(Index), new { status, highlight = id });
            }

            TempData["msg"] = "⚠️ Chỉ có thể thanh toán hóa đơn đang phục vụ.";
            return RedirectToAction(nameof(Index), new { status });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HuyHoaDon(int id, int status = 0)
        {
            var hd = await _db.HoaDons.FindAsync(id);
            if (hd == null) return NotFound();

            if (hd.TrangThaiId != 4)
            {
                hd.TrangThaiId = 5;

                if (hd.DatBanId.HasValue)
                {
                    var db = await _db.DatBans.FindAsync(hd.DatBanId.Value);
                    if (db != null) db.TrangThaiId = 5;
                }

                await _db.SaveChangesAsync();

                TempData["msg"] = $"🗑 Đã hủy hóa đơn #{id}.";
                return RedirectToAction(nameof(Index), new { status, highlight = id });
            }

            TempData["msg"] = "⚠️ Không thể hủy hóa đơn đã thanh toán.";
            return RedirectToAction(nameof(Index), new { status });
        }

        public async Task<IActionResult> Create(int? datBanId, int status = 0)
        {
            if (datBanId == null) return BadRequest("Thiếu DatBanID");

            var datBan = await _db.DatBans.FindAsync(datBanId);
            if (datBan == null) return NotFound();

            var hd = new HoaDon
            {
                DatBanId = datBan.DatBanId,
                NgayLap = DateTime.UtcNow,
                TongTien = 0,
                TrangThaiId = 1
            };

            _db.HoaDons.Add(hd);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Edit), new { id = hd.HoaDonId, status });
        }

        // ==============================
        // EDIT (VIEW + LOAD DATA)
        // ==============================
        public async Task<IActionResult> Edit(int id, int status = 0)
        {
            ViewBag.Status = status;

            var hd = await _db.HoaDons
                .Include(h => h.ChiTietHoaDons).ThenInclude(ct => ct.MonAn)
                .Include(h => h.DatBan).ThenInclude(db => db.KhungGio)
                .Include(h => h.DatBan).ThenInclude(db => db.BanPhong)
                .Include(h => h.TrangThai)
                .FirstOrDefaultAsync(h => h.HoaDonId == id);

            if (hd == null) return NotFound();

            var vm = new InvoiceEditVM
            {
                HoaDonID = hd.HoaDonId,
                DatBanID = hd.DatBanId ?? 0,
                BanPhongID = hd.DatBan != null ? (hd.DatBan.BanPhongId ?? 0) : 0,
                TrangThai = hd.TrangThai != null ? hd.TrangThai.TenTrangThai : "N/A",
                Items = hd.ChiTietHoaDons.Select(ct => new InvoiceEditVM.ItemLine
                {
                    MonAnID = ct.MonAnId,
                    TenMon = ct.MonAn != null ? ct.MonAn.TenMon : "Món đã xóa",
                    SoLuong = ct.SoLuong ?? 1,
                    DonGia = ct.DonGia ?? 0m
                }).ToList()
            };

            ViewBag.MonAn = await _db.MonAns.Where(m => m.TrangThai == "Còn bán").ToListAsync();
            ViewBag.BanPhongs = await _db.BanPhongs.ToListAsync();
            ViewBag.TrangThai = hd.TrangThai != null ? hd.TrangThai.TenTrangThai : "";
            ViewBag.DaThanhToan = (hd.TrangThaiId == 4);

            if (hd.DatBan != null && hd.DatBan.NgayDen != null)
            {
                ViewBag.NgayDenVal = string.Format("{0:yyyy-MM-dd}", hd.DatBan.NgayDen);
                ViewBag.KhungGioIdVal = hd.DatBan.KhungGioId;
            }

            ViewBag.ListKhungGio = await _db.KhungGios.ToListAsync();
            return View(vm);
        }

        // ==============================
        // SAVE (POST) - CẬP NHẬT HÓA ĐƠN
        // ==============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(InvoiceEditVM vm, int status = 0, string ItemsJson = "", DateTime? NewNgayDen = null, int? NewKhungGioID = null)
        {
            var hd = await _db.HoaDons
                .Include(h => h.ChiTietHoaDons)
                .Include(h => h.DatBan)
                .FirstOrDefaultAsync(h => h.HoaDonId == vm.HoaDonID);

            if (hd == null) return NotFound();

            if (hd.TrangThaiId == 4)
            {
                TempData["msg"] = "❌ Hóa đơn đã thanh toán, bạn không thể chỉnh sửa thông tin!";
                return RedirectToAction(nameof(Edit), new { id = vm.HoaDonID, status });
            }

            if (hd.DatBan != null)
            {
                hd.DatBan.BanPhongId = vm.BanPhongID == 0 ? null : vm.BanPhongID;

                if (NewNgayDen.HasValue)
                {
                    hd.DatBan.NgayDen = DateOnly.FromDateTime(NewNgayDen.Value);
                }

                if (NewKhungGioID.HasValue)
                {
                    hd.DatBan.KhungGioId = NewKhungGioID.Value;
                }
            }

            if (!string.IsNullOrEmpty(ItemsJson))
            {
                var items = JsonSerializer.Deserialize<List<InvoiceEditVM.ItemLine>>(ItemsJson);

                foreach (var old in hd.ChiTietHoaDons.ToList())
                {
                    _db.ChiTietHoaDons.Remove(old);
                }

                if (items != null)
                {
                    foreach (var item in items)
                    {
                        _db.ChiTietHoaDons.Add(new ChiTietHoaDon
                        {
                            HoaDonId = hd.HoaDonId,
                            MonAnId = item.MonAnID,
                            SoLuong = item.SoLuong,
                            DonGia = item.DonGia
                        });
                    }
                }
            }

            await _db.SaveChangesAsync();
            await UpdateTongTienAsync(hd);
            await _db.SaveChangesAsync();

            TempData["msg"] = "💾 Đã lưu thay đổi hóa đơn.";
            return RedirectToAction(nameof(Edit), new { id = vm.HoaDonID, status });
        }

        // =======================================
        // API: LẤY DANH SÁCH BÀN PHÒNG
        // =======================================
        [HttpGet]
        public IActionResult GetBanPhong(int hoaDonId)
        {
            var currentHD = _db.HoaDons
                .Include(h => h.DatBan)
                .FirstOrDefault(h => h.HoaDonId == hoaDonId);

            if (currentHD == null || currentHD.DatBan == null || currentHD.DatBan.NgayDen == null)
                return BadRequest("Lỗi dữ liệu đặt bàn.");

            var checkDate = currentHD.DatBan.NgayDen.Value;
            var checkSlot = currentHD.DatBan.KhungGioId;

            var banDangDuocSuDung = _db.HoaDons
                .Include(h => h.DatBan)
                .Where(h => h.TrangThaiId != 4 && h.TrangThaiId != 5)
                .Where(h => h.HoaDonId != hoaDonId)
                .Where(h => h.DatBan != null
                            && h.DatBan.NgayDen != null
                            && h.DatBan.NgayDen.Value == checkDate
                            && h.DatBan.KhungGioId == checkSlot)
                .Select(h => h.DatBan!.BanPhongId)
                .Where(id => id != null)
                .Distinct()
                .ToList();

            var list = _db.BanPhongs
                .Include(b => b.LoaiBanPhong)
                .Select(b => new
                {
                    banPhongID = b.BanPhongId,
                    tenBanPhong = b.TenBanPhong,
                    soChoNgoi = b.SucChua,
                    loai = b.LoaiBanPhong != null ? b.LoaiBanPhong.TenLoai : "Khác",
                    trangThai = banDangDuocSuDung.Contains(b.BanPhongId) ? "Bận" : "Trống"
                })
                .ToList();

            return Json(list);
        }

        private Task UpdateTongTienAsync(HoaDon hd)
        {
            var sub = hd.ChiTietHoaDons.Sum(x => x.ThanhTien.GetValueOrDefault(0m));
            var vat = sub * ((hd.Vatpercent ?? 10m) / 100m);
            var final = sub + vat;

            if (final < 0) final = 0;
            hd.TongTien = final;
            return Task.CompletedTask;
        }

        public async Task<IActionResult> Details(int? id, int status = 0)
        {
            if (id == null) return NotFound();

            var hoadon = await _db.HoaDons
                .Include(h => h.TrangThai)
                .Include(h => h.DatBan).ThenInclude(d => d.KhachHang)
                .Include(h => h.DatBan).ThenInclude(d => d.BanPhong).ThenInclude(bp => bp.LoaiBanPhong)
                .Include(h => h.ChiTietHoaDons).ThenInclude(c => c.MonAn)
                .FirstOrDefaultAsync(m => m.HoaDonId == id);

            if (hoadon == null) return NotFound();

            ViewBag.Status = status;
            ViewBag.ReturnUrl = Url.Action("Index", new { status });

            return View(hoadon);
        }

        public async Task<IActionResult> Print(int? id, int status = 0)
        {
            if (id == null) return NotFound();

            var hoadon = await _db.HoaDons
                .Include(h => h.TrangThai)
                .Include(h => h.DatBan).ThenInclude(d => d.KhachHang)
                .Include(h => h.ChiTietHoaDons).ThenInclude(c => c.MonAn)
                .FirstOrDefaultAsync(m => m.HoaDonId == id);

            if (hoadon == null) return NotFound();

            if (hoadon.DatBan != null)
            {
                ViewBag.Khachhang = hoadon.DatBan.KhachHang;
            }

            ViewBag.Status = status;
            return View(hoadon);
        }

        [HttpGet]
        public async Task<IActionResult> CreateWalkIn(int status = 0)
        {
            ViewBag.Status = status;

            var vm = new InvoiceCreateVM
            {
                NgayDen = DateTime.UtcNow
            };

            int hour = DateTime.UtcNow.Hour;
            string tenKhungGio = hour < 15 ? "Trưa" : "Tối";

            var kg = await _db.KhungGios.FirstOrDefaultAsync(k => k.TenKhungGio == tenKhungGio);
            if (kg != null) vm.KhungGioID = kg.KhungGioId;

            ViewBag.ListKhungGio = await _db.KhungGios
                .Where(k => k.TenKhungGio == "Trưa" || k.TenKhungGio == "Tối")
                .OrderBy(k => k.KhungGioId)
                .ToListAsync();

            ViewBag.BanPhongs = await _db.BanPhongs
                .Include(b => b.LoaiBanPhong)
                .ToListAsync();

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWalkIn(InvoiceCreateVM vm, int status = 0)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Status = status;
                ViewBag.ListKhungGio = await _db.KhungGios
                    .Where(k => k.TenKhungGio == "Trưa" || k.TenKhungGio == "Tối")
                    .OrderBy(k => k.KhungGioId)
                    .ToListAsync();

                ViewBag.BanPhongs = await _db.BanPhongs
                    .Include(b => b.LoaiBanPhong)
                    .ToListAsync();

                return View(vm);
            }

            using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var datBan = new DatBan
                {
                    KhachHangId = vm.KhachHangID,
                    BanPhongId = (vm.BanPhongID == null || vm.BanPhongID == 0) ? null : vm.BanPhongID,
                    KhungGioId = vm.KhungGioID,
                    NgayDen = DateOnly.FromDateTime(vm.NgayDen),
                    ThoiGianTaoDon = DateTime.UtcNow,
                    TrangThaiId = 3,
                    YeuCauDacBiet = "Tạo tại quầy"
                };

                _db.DatBans.Add(datBan);
                await _db.SaveChangesAsync();

                var hd = new HoaDon
                {
                    DatBanId = datBan.DatBanId,
                    NgayLap = DateTime.UtcNow,
                    TongTien = 0,
                    TrangThaiId = 3,
                    Vatpercent = 10m
                };

                int? staffId = HttpContext.Session.GetInt32("AccountId");
                if (staffId.HasValue)
                    hd.TaiKhoanId = staffId.Value;

                _db.HoaDons.Add(hd);
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["msg"] = "📝 Đã tạo đơn thành công. Vui lòng thêm món ăn!";
                return RedirectToAction(nameof(Edit), new { id = hd.HoaDonId, status = 3 });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError("", ex.InnerException?.Message ?? ex.Message);

                ViewBag.Status = status;
                ViewBag.ListKhungGio = await _db.KhungGios
                    .Where(k => k.TenKhungGio == "Trưa" || k.TenKhungGio == "Tối")
                    .OrderBy(k => k.KhungGioId)
                    .ToListAsync();

                ViewBag.BanPhongs = await _db.BanPhongs
                    .Include(b => b.LoaiBanPhong)
                    .ToListAsync();

                return View(vm);
            }
        }
    }
}