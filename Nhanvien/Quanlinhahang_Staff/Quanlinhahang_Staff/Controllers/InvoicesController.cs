using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quanlinhahang.Data.Models;
using Quanlinhahang_Staff.Models.ViewModels;
using System.Text.Json;

namespace Quanlinhahang_Staff.Controllers
{
    public class InvoicesController : Controller
    {
        private readonly QuanLyNhaHangContext _db;

        public InvoicesController(QuanLyNhaHangContext db)
        {
            _db = db;
        }

        // ==============================
        // LIST HÓA ĐƠN
        // ==============================
        public async Task<IActionResult> Index([FromQuery] InvoiceFilterVM f, [FromQuery] int status = 0)
        {
            ViewBag.Status = status;
            ViewBag.Filter = f;

            var query = _db.Hoadons
                .Include(h => h.Trangthai)
                .Include(h => h.Datban).ThenInclude(db => db.Khachhang)
                .Include(h => h.Banphong).ThenInclude(bp => bp!.Loaibanphong)
                .Include(h => h.Chitiethoadons)
                .AsQueryable();

            if (status > 0)
                query = query.Where(h => h.Trangthaiid == status);

            if (!string.IsNullOrWhiteSpace(f.Search))
            {
                string s = f.Search.Trim().ToLower();
                // [FIX CS8602]: Kiểm tra null trước khi truy cập thuộc tính con
                query = query.Where(h =>
                    (h.Datban != null && h.Datban.Khachhang != null && h.Datban.Khachhang.Hoten.ToLower().Contains(s)) ||
                    (h.Datban != null && h.Datban.Khachhang != null && (h.Datban.Khachhang.Sodienthoai ?? "").Contains(s)));
            }

            if (f.From.HasValue)
                query = query.Where(h => h.Ngaylap.Date >= f.From.Value.Date);

            if (f.To.HasValue)
                query = query.Where(h => h.Ngaylap.Date <= f.To.Value.Date);

            // SELECT → ViewModel
            var projectedData = query.Select(h => new
            {
                HoaDon = h,
                SubTotal = h.Chitiethoadons.Sum(ct => ct.Thanhtien)
            })
            .Select(x => new InvoiceRowVM
            {
                HoaDonID = x.HoaDon.Hoadonid,
                NgayLap = x.HoaDon.Ngaylap,
                // [FIX CS8602]: Dùng ?. để an toàn nếu Datban null
                NgayDen = x.HoaDon.Datban != null ? x.HoaDon.Datban.Ngayden : DateTime.MinValue,

                KhungGio = (x.HoaDon.Datban != null && x.HoaDon.Datban.Khunggio != null)
                           ? x.HoaDon.Datban.Khunggio.Tenkhunggio
                           : "---",

                // [FIX CS8602]: Thêm ?. và ?? "" để không bị lỗi null
                KhachHang = x.HoaDon.Datban != null && x.HoaDon.Datban.Khachhang != null
                            ? x.HoaDon.Datban.Khachhang.Hoten
                            : "Khách vãng lai",

                SoDienThoai = x.HoaDon.Datban != null && x.HoaDon.Datban.Khachhang != null
                              ? x.HoaDon.Datban.Khachhang.Sodienthoai
                              : "",

                BanPhong = x.HoaDon.Banphong != null ? x.HoaDon.Banphong.Tenbanphong : "",

                LoaiBanPhong = (x.HoaDon.Banphong != null && x.HoaDon.Banphong.Loaibanphong != null)
                    ? x.HoaDon.Banphong.Loaibanphong.Tenloai
                    : "",

                ThanhTien = (x.SubTotal * (1 + (x.HoaDon.Vat.HasValue ? x.HoaDon.Vat.Value : 0.10m))),

                TrangThaiID = x.HoaDon.Trangthaiid,

                // [FIX CS8602]: Trangthai có thể null về mặt lý thuyết
                TrangThaiTen = x.HoaDon.Trangthai != null ? x.HoaDon.Trangthai.Tentrangthai : "Không xác định"
            });

            var list = await projectedData
                .OrderBy(x => x.HoaDonID)
                .ToListAsync();

            return View(list);
        }

        // ... (Các hàm khác giữ nguyên vì không báo lỗi logic null ở đây) ...

        // ==============================
        // CHUYỂN SANG ĐANG PHỤC VỤ
        // ==============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartServing(int id, int status)
        {
            var hd = await _db.Hoadons.FindAsync(id);
            if (hd == null) return NotFound();

            if (hd.Trangthaiid == 2)
            {
                hd.Trangthaiid = 3;
                await _db.SaveChangesAsync();
                TempData["msg"] = "✅ Đã chuyển sang trạng thái phục vụ.";
            }
            else TempData["msg"] = "⚠️ Không thể phục vụ hóa đơn này.";

            return RedirectToAction(nameof(Index), new { status });
        }

        // ==============================
        // XÁC NHẬN HÓA ĐƠN
        // ==============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmInvoice(int id, int status)
        {
            var hd = await _db.Hoadons.FindAsync(id);
            if (hd == null) return NotFound();

            if (hd.Trangthaiid == 1)
            {
                hd.Trangthaiid = 2;
                await _db.SaveChangesAsync();
                TempData["msg"] = "✅ Hóa đơn đã được xác nhận.";
            }
            else TempData["msg"] = "⚠️ Không thể xác nhận hóa đơn này.";

            return RedirectToAction(nameof(Index), new { status });
        }

        // ==============================
        // HỦY HÓA ĐƠN
        // ==============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HuyHoaDon(int id, int status)
        {
            var hd = await _db.Hoadons.FindAsync(id);
            if (hd == null) return NotFound();

            if (hd.Trangthaiid != 4)
            {
                hd.Trangthaiid = 5;
                var db = await _db.Datbans.FindAsync(hd.Datbanid);
                if (db != null) db.Trangthai = "Đã hủy";

                await _db.SaveChangesAsync();
                TempData["msg"] = "🗑 Hóa đơn đã bị hủy.";
            }
            else TempData["msg"] = "⚠️ Không thể hủy hóa đơn đã thanh toán.";

            return RedirectToAction(nameof(Index), new { status });
        }

        // ==============================
        // THANH TOÁN HÓA ĐƠN
        // ==============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThanhToan(int id, int status)
        {
            var hd = await _db.Hoadons
                .Include(h => h.Chitiethoadons)
                .FirstOrDefaultAsync(h => h.Hoadonid == id);

            if (hd == null) return NotFound();

            if (hd.Trangthaiid == 3)
            {
                await UpdateTongTienAsync(hd);
                hd.Trangthaiid = 4;
                await _db.SaveChangesAsync();
                TempData["msg"] = "💰 Đã thanh toán.";
            }
            else TempData["msg"] = "⚠️ Chỉ thanh toán hóa đơn đang phục vụ.";

            return RedirectToAction(nameof(Index), new { status });
        }

        // ==============================
        // TẠO HÓA ĐƠN
        // ==============================
        public async Task<IActionResult> Create(int? datBanId)
        {
            if (datBanId == null) return BadRequest("Thiếu DatBanID");

            var datBan = await _db.Datbans.FindAsync(datBanId);
            if (datBan == null) return NotFound();

            var hd = new Hoadon
            {
                Datbanid = datBan.Datbanid,
                Ngaylap = DateTime.Now,
                Tongtien = 0,
                Trangthaiid = 1
            };

            _db.Hoadons.Add(hd);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Edit), new { id = hd.Hoadonid });
        }

        // ==============================
        // EDIT (VIEW + LOAD DATA)
        // ==============================
        public async Task<IActionResult> Edit(int id, int status = 0)
        {
            ViewBag.Status = status;

            var hd = await _db.Hoadons
                .Include(h => h.Chitiethoadons).ThenInclude(ct => ct.Monan)
                .Include(h => h.Datban).ThenInclude(db => db.Khunggio)
                .Include(h => h.Datban)
                .Include(h => h.Trangthai)
                .FirstOrDefaultAsync(h => h.Hoadonid == id);

            if (hd == null) return NotFound();

            var vm = new InvoiceEditVM
            {
                HoaDonID = hd.Hoadonid,
                DatBanID = hd.Datbanid,
                BanPhongID = hd.Banphongid,
                HinhThucThanhToan = hd.Hinhthucthanhtoan,
                TrangThai = hd.Trangthai != null ? hd.Trangthai.Tentrangthai : "N/A", // Fix null

                Items = hd.Chitiethoadons.Select(ct => new InvoiceEditVM.ItemLine
                {
                    MonAnID = ct.Monanid,
                    TenMon = ct.Monan != null ? ct.Monan.Tenmon : "Món đã xóa", // Fix null
                    SoLuong = ct.Soluong,
                    DonGia = ct.Dongia
                }).ToList()
            };

            ViewBag.MonAn = await _db.Monans.Where(m => m.Trangthai == "Còn bán").ToListAsync();
            ViewBag.BanPhongs = await _db.Banphongs.ToListAsync();
            ViewBag.TrangThai = hd.Trangthai != null ? hd.Trangthai.Tentrangthai : "";
            ViewBag.DaThanhToan = (hd.Trangthaiid == 4);
            if (hd.Datban != null)
            {
                ViewBag.NgayDenVal = hd.Datban.Ngayden.ToString("yyyy-MM-dd");
                ViewBag.KhungGioIdVal = hd.Datban.Khunggioid;
            }

            ViewBag.ListKhungGio = await _db.Khunggios.ToListAsync();
            return View(vm);
        }

        // =======================================
        // LẤY DANH SÁCH BÀN PHÒNG (API AJAX)
        // =======================================
        [HttpGet]
        public IActionResult GetBanPhong(int hoaDonId)
        {
            var currentHD = _db.Hoadons
                .Include(h => h.Datban)
                .FirstOrDefault(h => h.Hoadonid == hoaDonId);

            if (currentHD == null || currentHD.Datban == null)
                return BadRequest("Không tìm thấy thông tin đặt bàn.");

            var checkDate = currentHD.Datban.Ngayden.Date;
            var checkSlot = currentHD.Datban.Khunggioid;

            var banDangDuocSuDung = _db.Hoadons
                .Include(h => h.Datban)
                .Where(h => h.Trangthaiid != 4 && h.Trangthaiid != 5)
                .Where(h => h.Hoadonid != hoaDonId)
                .Where(h => h.Datban.Ngayden.Date == checkDate)
                .Where(h => h.Datban.Khunggioid == checkSlot)
                .Select(h => h.Banphongid)
                .Where(id => id != null)
                .Distinct()
                .ToList();

            var list = _db.Banphongs
                .Include(b => b.Loaibanphong)
                .Select(b => new
                {
                    banPhongID = b.Banphongid,
                    tenBanPhong = b.Tenbanphong,
                    soChoNgoi = b.Succhua,
                    loai = b.Loaibanphong.Tenloai,
                    trangThai = banDangDuocSuDung.Contains(b.Banphongid) ? "Bận" : "Trống"
                })
                .ToList();

            return Json(list);
        }

        // ==============================
        // SAVE (POST)
        // ==============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(InvoiceEditVM vm, int status, string ItemsJson, DateTime? NewNgayDen, int? NewKhungGioID)
        {
            var hd = await _db.Hoadons
                .Include(h => h.Chitiethoadons)
                .Include(h => h.Datban)
                .FirstOrDefaultAsync(h => h.Hoadonid == vm.HoaDonID);

            if (hd == null) return NotFound();

            hd.Banphongid = vm.BanPhongID;
            hd.Hinhthucthanhtoan = vm.HinhThucThanhToan;

            if (hd.Datban != null)
            {
                if (NewNgayDen.HasValue) hd.Datban.Ngayden = NewNgayDen.Value;
                if (NewKhungGioID.HasValue) hd.Datban.Khunggioid = NewKhungGioID.Value;
            }

            if (!string.IsNullOrEmpty(ItemsJson))
            {
                var items = JsonSerializer.Deserialize<List<InvoiceEditVM.ItemLine>>(ItemsJson);

                foreach (var old in hd.Chitiethoadons.ToList()) { _db.Chitiethoadons.Remove(old); }

                if (items != null) // Check null cho items
                {
                    foreach (var item in items)
                    {
                        _db.Chitiethoadons.Add(new Chitiethoadon
                        {
                            Hoadonid = hd.Hoadonid,
                            Monanid = item.MonAnID,
                            Soluong = item.SoLuong,
                            Dongia = (long)item.DonGia,
                            Thanhtien = (long)(item.SoLuong * item.DonGia)
                        });
                    }
                }
            }

            await UpdateTongTienAsync(hd);
            await _db.SaveChangesAsync();

            TempData["msg"] = "Đã lưu hóa đơn.";
            return RedirectToAction(nameof(Edit), new { id = vm.HoaDonID, status });
        }

        // ==============================
        // HELPER: CẬP NHẬT TỔNG TIỀN
        // ==============================
        private Task UpdateTongTienAsync(Hoadon hd)
        {
            var sub = hd.Chitiethoadons.Sum(x => x.Thanhtien);
            var vat = sub * 0.1m;
            var final = sub + vat;
            if (final < 0) final = 0;
            hd.Tongtien = (long)final;
            return Task.CompletedTask;
        }
    }
}