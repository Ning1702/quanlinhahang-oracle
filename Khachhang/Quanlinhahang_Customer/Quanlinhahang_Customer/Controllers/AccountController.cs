using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quanlinhahang.Data.Models;
using Quanlinhahang_Customer.Models.ViewModels;
using System.Security.Cryptography;
using System.Text;

namespace Quanlinhahang_Customer.Controllers
{
    [Route("Account")]
    public class AccountController : Controller
    {
        private readonly QuanLyNhaHangContext _context;

        public AccountController(QuanLyNhaHangContext context)
        {
            _context = context;
        }

        [HttpGet("Dangki")] public IActionResult Dangki() => View();
        [HttpGet("Info")] public IActionResult Info() => View();
        [HttpGet("History")] public IActionResult History() => View();
        [HttpGet("Vouchers")] public IActionResult Vouchers() => View();

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });

            string usernameInput = (model.Username ?? "").ToUpper();

            if (await _context.TaiKhoans.AnyAsync(t => (t.TenDangNhap ?? "").ToUpper() == usernameInput))
                return Conflict(new { success = false, message = "Tên đăng nhập này đã được sử dụng." });

            var hashedPassword = HashPassword(model.Password);

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var taiKhoan = new TaiKhoan
                    {
                        TenDangNhap = model.Username ?? "",
                        MatKhauHash = hashedPassword,
                        Email = model.Email ?? "",
                        VaiTro = VaiTroHeThong.Customer.ToString(),
                        TrangThai = "Hoạt động"
                    };
                    _context.TaiKhoans.Add(taiKhoan);
                    await _context.SaveChangesAsync();

                    var khachHang = new KhachHang
                    {
                        HoTen = model.FullName ?? "Khách hàng",
                        Email = model.Email ?? "",
                        SoDienThoai = model.Phone ?? "",
                        DiaChi = model.Address ?? "",
                        DiemTichLuy = 0,
                        TaiKhoanId = taiKhoan.TaiKhoanId,
                        NgayTao = DateTime.UtcNow
                        // Đã xóa KhachHang.TrangThai vì SQL mới không có cột này
                    };
                    _context.KhachHangs.Add(khachHang);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    return Json(new { success = true, message = "Đăng ký thành công! Vui lòng đăng nhập." });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { success = false, message = "Lỗi: " + ex.Message });
                }
            }
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
                return BadRequest(new { success = false, message = "Vui lòng nhập đầy đủ thông tin." });

            string input = model.Username.Trim().ToUpper();

            var taiKhoan = await _context.TaiKhoans
                .FirstOrDefaultAsync(t => (t.TenDangNhap ?? "").ToUpper() == input
                                          || (t.Email != null && t.Email.ToUpper() == input));

            if (taiKhoan == null)
            {
                var khachHang = await _context.KhachHangs
                    .Include(k => k.TaiKhoan)
                    .FirstOrDefaultAsync(k => k.SoDienThoai == input && k.TaiKhoanId != null);

                if (khachHang != null && khachHang.TaiKhoan != null)
                {
                    taiKhoan = khachHang.TaiKhoan;
                }
            }

            if (taiKhoan == null || taiKhoan.MatKhauHash != HashPassword(model.Password) || taiKhoan.TrangThai != "Hoạt động")
            {
                return Unauthorized(new { success = false, message = "Tài khoản hoặc mật khẩu không đúng." });
            }

            string fullName = taiKhoan.TenDangNhap ?? "User";
            if (taiKhoan.VaiTro == VaiTroHeThong.Customer.ToString())
            {
                var kh = await _context.KhachHangs.FirstOrDefaultAsync(k => k.TaiKhoanId == taiKhoan.TaiKhoanId);
                if (kh != null) fullName = kh.HoTen ?? fullName;
            }
            else
            {
                var nv = await _context.NhanViens.FirstOrDefaultAsync(n => n.TaiKhoanId == taiKhoan.TaiKhoanId);
                if (nv != null) fullName = nv.HoTen ?? fullName;
            }

            return Json(new { success = true, user = new { username = taiKhoan.TenDangNhap, fullName, role = taiKhoan.VaiTro.ToString() } });
        }

        [HttpPost("CheckUsername")]
        public async Task<IActionResult> CheckUsername([FromBody] CheckUsernameViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Username)) return BadRequest(new { success = false });
            string usernameInput = model.Username.ToUpper();

            var exists = await _context.TaiKhoans.AnyAsync(t => (t.TenDangNhap ?? "").ToUpper() == usernameInput || (t.Email != null && t.Email.ToUpper() == usernameInput));
            if (!exists) exists = await _context.KhachHangs.AnyAsync(k => k.SoDienThoai == model.Username);

            if (!exists) return Json(new { success = false, message = "Tài khoản không tồn tại." });
            return Json(new { success = true });
        }

        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            if (!ModelState.IsValid) return BadRequest(new { success = false });
            string usernameInput = (model.Username ?? "").ToUpper();

            var taiKhoan = await _context.TaiKhoans.FirstOrDefaultAsync(t => (t.TenDangNhap ?? "").ToUpper() == usernameInput || (t.Email != null && t.Email.ToUpper() == usernameInput));
            if (taiKhoan == null)
            {
                var khachHang = await _context.KhachHangs.Include(k => k.TaiKhoan).FirstOrDefaultAsync(k => k.SoDienThoai == model.Username);
                if (khachHang != null) taiKhoan = khachHang.TaiKhoan;
            }

            if (taiKhoan == null) return NotFound(new { success = false, message = "Không tìm thấy tài khoản." });

            taiKhoan.MatKhauHash = HashPassword(model.NewPassword);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đổi mật khẩu thành công!" });
        }

        [HttpGet("GetUserInfo")]
        public async Task<IActionResult> GetUserInfo([FromQuery] string username)
        {
            if (string.IsNullOrEmpty(username)) return BadRequest(new { success = false });

            var khachHang = await _context.KhachHangs.Include(k => k.TaiKhoan)
                .FirstOrDefaultAsync(k => k.TaiKhoan != null && (k.TaiKhoan.TenDangNhap ?? "").ToUpper() == username.ToUpper());

            if (khachHang == null)
            {
                var nhanVien = await _context.NhanViens.Include(n => n.TaiKhoan)
                    .FirstOrDefaultAsync(n => n.TaiKhoan != null && (n.TaiKhoan.TenDangNhap ?? "").ToUpper() == username.ToUpper());
                if (nhanVien != null) return Json(new { fullName = nhanVien.HoTen, email = nhanVien.TaiKhoan?.Email, phone = nhanVien.SoDienThoai, address = "N/A" });
                return NotFound(new { success = false, message = "Không tìm thấy thông tin." });
            }
            return Json(new { fullName = khachHang.HoTen, email = khachHang.Email, phone = khachHang.SoDienThoai, address = khachHang.DiaChi });
        }

        [HttpPost("UpdateUserInfo")]
        public async Task<IActionResult> UpdateUserInfo([FromBody] UpdateInfoViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(new { success = false });
            var khachHang = await _context.KhachHangs.Include(k => k.TaiKhoan)
                .FirstOrDefaultAsync(k => k.TaiKhoan != null && (k.TaiKhoan.TenDangNhap ?? "").ToUpper() == (model.Username ?? "").ToUpper());

            if (khachHang == null) return NotFound(new { success = false });
            var taiKhoan = khachHang.TaiKhoan;

            if (taiKhoan == null) return NotFound(new { success = false });

            if (taiKhoan.TenDangNhap != model.Phone)
            {
                if (await _context.TaiKhoans.AnyAsync(t => (t.TenDangNhap ?? "").ToUpper() == (model.Phone ?? "").ToUpper()))
                    return Conflict(new { success = false, message = "Số điện thoại mới đã tồn tại." });
                taiKhoan.TenDangNhap = model.Phone;
            }
            khachHang.HoTen = model.FullName;
            khachHang.Email = model.Email;
            khachHang.SoDienThoai = model.Phone;
            khachHang.DiaChi = model.Address;
            if (taiKhoan.Email != model.Email) taiKhoan.Email = model.Email;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cập nhật thành công!", newFullName = khachHang.HoTen, newUsername = taiKhoan.TenDangNhap });
        }

        [HttpGet("GetHistoryData")]
        public async Task<IActionResult> GetHistoryData([FromQuery] string username, [FromQuery] string status)
        {
            if (string.IsNullOrEmpty(username)) return BadRequest(new { success = false });
            var taiKhoan = await _context.TaiKhoans.FirstOrDefaultAsync(t => (t.TenDangNhap ?? "").ToUpper() == username.ToUpper());
            if (taiKhoan == null) return NotFound(new { success = false });

            // SỬA: Đổi Hoadons thành HoaDon, bao gồm TrangThai của DatBan (nếu EF map là TrangThaiDatBan thì bạn đổi tên theo EF nhé)
            IQueryable<DatBan> query = _context.DatBans
                .Include(d => d.BanPhong)
                .Include(d => d.KhachHang)
                .Include(d => d.HoaDon).ThenInclude(h => h.TrangThai);

            if (taiKhoan.VaiTro == VaiTroHeThong.Customer.ToString())
            {
                var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.TaiKhoanId == taiKhoan.TaiKhoanId);
                if (khachHang != null) query = query.Where(d => d.KhachHangId == khachHang.KhachHangId);
            }

            int? trangThaiId = null;
            bool filterHuy = false;
            bool filterCho = false;

            switch (status?.ToLower() ?? "")
            {
                case "chưa xác nhận": filterCho = true; break;
                case "đã xác nhận": trangThaiId = 2; break;
                case "đang phục vụ": trangThaiId = 3; break;
                case "đã thanh toán": trangThaiId = 4; break;
                case "đã hủy": filterHuy = true; trangThaiId = 5; break;
            }

            // Lọc theo TrangThaiId (SQL mới là INT)
            if (filterHuy)
                query = query.Where(d => d.TrangThaiId == 5 || (d.HoaDon != null && d.HoaDon.TrangThaiId == (trangThaiId ?? 5)));
            else if (trangThaiId.HasValue)
                query = query.Where(d => d.HoaDon != null && d.HoaDon.TrangThaiId == trangThaiId.GetValueOrDefault());
            else if (filterCho)
                query = query.Where(d => d.TrangThaiId == 1); // 1 = Chờ xác nhận

            // Chú ý d.TrangThai.TenTrangThai có thể null nếu bạn chưa include bảng TrangThaiDatBan
            var list = await query.OrderByDescending(d => d.NgayDen)
                .Select(d => new {
                    datBanId = d.DatBanId,
                    ngayDen = d.NgayDen.HasValue ? d.NgayDen.Value.ToString("dd/MM/yyyy") : "",
                    tenKhachHang = d.KhachHang != null ? d.KhachHang.HoTen : "Ẩn danh",
                    tenBanPhong = d.BanPhong != null ? d.BanPhong.TenBanPhong : "N/A",
                    soNguoi = d.SoNguoi,
                    trangThaiDatBan = (d.TrangThaiId == 1) ? "Chờ xác nhận" : ((d.TrangThaiId == 5) ? "Đã hủy" : "Đã xử lý"),
                    trangThaiHoaDon = d.HoaDon != null ? d.HoaDon.TrangThai.TenTrangThai : null
                }).ToListAsync();

            return Json(new { success = true, list });
        }

        [HttpGet("GetUserVouchers")]
        public async Task<IActionResult> GetUserVouchers([FromQuery] string username)
        {
            if (string.IsNullOrEmpty(username)) return BadRequest(new { success = false });
            var taiKhoan = await _context.TaiKhoans.FirstOrDefaultAsync(t => (t.TenDangNhap ?? "").ToUpper() == username.ToUpper());
            if (taiKhoan == null) return NotFound(new { success = false, message = "Tài khoản lỗi." });

            var khachHang = await _context.KhachHangs.Include(k => k.HangThanhVien).FirstOrDefaultAsync(k => k.TaiKhoanId == taiKhoan.TaiKhoanId);
            if (khachHang == null) return NotFound(new { success = false, message = "Không tìm thấy thông tin khách hàng." });

            string hangThanhVien = khachHang.HangThanhVien?.TenHang ?? "Thường";

            int diem = Convert.ToInt32(khachHang.DiemTichLuy);

            var vouchers = new List<object>();
            string ex30 = DateTime.UtcNow.AddDays(30).ToString("dd/MM/yyyy");
            string ex90 = DateTime.UtcNow.AddMonths(3).ToString("dd/MM/yyyy");
            string ex365 = DateTime.UtcNow.AddYears(1).ToString("dd/MM/yyyy");

            vouchers.Add(new { code = "WELCOME10", value = 10, type = "Phần trăm", minOrder = 200000, expiry = ex30, description = "Giảm 10% cho đơn hàng đầu tiên." });
            if (diem >= 1000) vouchers.Add(new { code = "FREE_DRINK", value = 1, type = "Món ăn", minOrder = 0, expiry = ex90, description = $"Tặng 1 đồ uống miễn phí (Hạng {hangThanhVien})." });
            if (hangThanhVien.Contains("Kim cương")) vouchers.Add(new { code = "KIMCUONG20", value = 20, type = "Phần trăm", minOrder = 500000, expiry = ex365, description = "Giảm 20% đặc biệt cho khách hạng Kim cương." });

            return Json(new { success = true, diemTichLuy = diem, hangThanhVien, list = vouchers });
        }

        private async Task<int> ResolveKhungGioId(string? timeSlot)
        {
            string key = (timeSlot ?? "").Trim().ToLower();
            if (key.Contains("trua") || key.Contains("trưa")) key = "trưa";
            else if (key.Contains("toi") || key.Contains("tối")) key = "tối";
            else return 0;

            var khungGio = await _context.KhungGios.FirstOrDefaultAsync(k => k.TenKhungGio.ToLower() == key);

            return khungGio != null ? (int)khungGio.KhungGioId : 0;
        }

        private string HashPassword(string password)
        {
            using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }

        [HttpGet("HistoryDetail/{datBanId}")]
        public async Task<IActionResult> HistoryDetail(int datBanId)
        {
            var datBan = await _context.DatBans
                .Include(d => d.KhungGio)
                .Include(d => d.BanPhong)
                .Include(d => d.KhachHang)
                .Include(d => d.HoaDon).ThenInclude(h => h.TrangThai)
                .Include(d => d.HoaDon).ThenInclude(h => h.ChiTietHoaDons).ThenInclude(ct => ct.MonAn)
                .FirstOrDefaultAsync(d => d.DatBanId == datBanId);

            if (datBan == null) return NotFound("Không tìm thấy đơn đặt bàn.");

            ViewBag.DanhSachBan = await _context.BanPhongs.Include(b => b.LoaiBanPhong).OrderBy(b => b.BanPhongId).ToListAsync();
            ViewBag.DanhSachMonAn = await _context.MonAns.Where(m => m.TrangThai == "Còn bán").Include(m => m.DanhMuc).OrderBy(m => m.DanhMucId).ToListAsync();
            return View(datBan);
        }

        [HttpPost("UpdateBooking")]
        public async Task<IActionResult> UpdateBooking([FromBody] UpdateBookingViewModel model)
        {
            if (model == null) return BadRequest(new { success = false, message = "Lỗi: Server không đọc được dữ liệu gửi lên (JSON Null)." });

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ: " + errors });
            }

            var datBan = await _context.DatBans
                .Include(d => d.HoaDon).ThenInclude(h => h.ChiTietHoaDons)
                .FirstOrDefaultAsync(d => d.DatBanId == model.DatBanId);

            if (datBan == null) return NotFound(new { success = false, message = "Không tìm thấy đơn đặt bàn." });

            var hoaDon = datBan.HoaDon;
            // 1: Chờ xác nhận
            if (datBan.TrangThaiId != 1 && (hoaDon != null && hoaDon.TrangThaiId != 1))
                return BadRequest(new { success = false, message = "Không thể sửa đơn này do trạng thái không hợp lệ." });

            if (!DateOnly.TryParse(model.BookingDate, out DateOnly bookingDate))
                return BadRequest(new { success = false, message = "Ngày đặt không hợp lệ." });

            int khungGioId = await ResolveKhungGioId(model.TimeSlot);
            if (khungGioId == 0) return BadRequest(new { success = false, message = "Khung giờ lỗi." });

            if (model.BanPhongId.HasValue && model.BanPhongId != datBan.BanPhongId)
            {
                var banMoi = await _context.BanPhongs.FindAsync(model.BanPhongId.GetValueOrDefault());
                // 0: Trống
                if (banMoi == null || banMoi.TrangThaiId != 0)
                    return BadRequest(new { success = false, message = "Bàn đã chọn không còn trống." });
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    datBan.NgayDen = bookingDate;
                    datBan.KhungGioId = khungGioId;
                    datBan.SoNguoi = model.GuestCount;
                    datBan.BanPhongId = model.BanPhongId;
                    // Đã xóa datBan.TongTienDuKien vì SQL mới không còn cột này

                    if (hoaDon != null)
                    {
                        _context.ChiTietHoaDons.RemoveRange(hoaDon.ChiTietHoaDons);
                        await _context.SaveChangesAsync();

                        decimal newTotal = 0;
                        if (model.Items != null)
                        {
                            foreach (var item in model.Items)
                            {
                                _context.ChiTietHoaDons.Add(new ChiTietHoaDon
                                {
                                    HoaDonId = hoaDon.HoaDonId,
                                    MonAnId = item.MonAnId,
                                    SoLuong = item.SoLuong,
                                    DonGia = (decimal)item.DonGia
                                    // Bỏ gán ThanhTien vì DB đã cấu hình AS (SoLuong * DonGia) PERSISTED
                                });
                                newTotal += (decimal)(item.DonGia * item.SoLuong);
                            }
                        }
                        hoaDon.TongTien = newTotal;
                    }
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return Json(new { success = true, message = "Cập nhật thành công!" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { success = false, message = "Lỗi Server: " + ex.Message });
                }
            }
        }

        [HttpPost("CancelBooking")]
        public async Task<IActionResult> CancelBooking([FromBody] CancelBookingRequest req)
        {
            var taiKhoan = await _context.TaiKhoans.FirstOrDefaultAsync(t => (t.TenDangNhap ?? "").ToUpper() == (req.Username ?? "").ToUpper());
            if (taiKhoan == null) return Unauthorized(new { success = false });

            var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k => k.TaiKhoanId == taiKhoan.TaiKhoanId);
            if (khachHang == null) return Unauthorized(new { success = false });

            var datBan = await _context.DatBans.Include(d => d.HoaDon).FirstOrDefaultAsync(d => d.DatBanId == req.DatBanId && d.KhachHangId == khachHang.KhachHangId);
            if (datBan == null) return NotFound(new { success = false });

            // 1: Chờ xác nhận
            if (datBan.TrangThaiId != 1) return BadRequest(new { success = false, message = "Không thể hủy." });

            datBan.TrangThaiId = 5; // 5: Đã hủy

            var hoaDon = datBan.HoaDon;
            if (hoaDon != null)
            {
                hoaDon.TrangThaiId = 5;
                _context.HoaDons.Update(hoaDon);
            }

            _context.DatBans.Update(datBan);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã hủy đơn." });
        }
    }
}