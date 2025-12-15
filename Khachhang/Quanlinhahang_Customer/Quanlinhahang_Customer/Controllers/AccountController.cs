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

            // Fix CS8602: Thêm ?? "" để tránh null
            string usernameInput = (model.Username ?? "").ToUpper();

            // Fix CS8602: Kiểm tra null trong DB
            if (await _context.Taikhoans.AnyAsync(t => (t.Tendangnhap ?? "").ToUpper() == usernameInput))
                return Conflict(new { success = false, message = "Tên đăng nhập này đã được sử dụng." });

            var hashedPassword = HashPassword(model.Password);

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Fix CS8601: Đảm bảo không gán null vào trường bắt buộc
                    var taiKhoan = new Taikhoan
                    {
                        Tendangnhap = model.Username ?? "",
                        Matkhauhash = hashedPassword,
                        Email = model.Email ?? "",
                        Vaitro = VaiTroHeThong.Customer,
                        Trangthai = "Hoạt động"
                    };
                    _context.Taikhoans.Add(taiKhoan);
                    await _context.SaveChangesAsync();

                    var khachHang = new Khachhang
                    {
                        Hoten = model.FullName ?? "Khách hàng",
                        Email = model.Email ?? "",
                        Sodienthoai = model.Phone ?? "",
                        Diachi = model.Address ?? "",
                        Diemtichluy = 0,
                        Taikhoanid = (int)taiKhoan.Taikhoanid,
                        Ngaytao = DateTime.Now,
                        Trangthai = "Hoạt động"
                    };
                    _context.Khachhangs.Add(khachHang);
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

            var taiKhoan = await _context.Taikhoans
                .FirstOrDefaultAsync(t => (t.Tendangnhap ?? "").ToUpper() == input
                                          || (t.Email != null && t.Email.ToUpper() == input));

            if (taiKhoan == null)
            {
                var khachHang = await _context.Khachhangs
                    .Include(k => k.Taikhoan)
                    .FirstOrDefaultAsync(k => k.Sodienthoai == input && k.Taikhoanid != null);

                if (khachHang != null && khachHang.Taikhoan != null)
                {
                    taiKhoan = khachHang.Taikhoan;
                }
            }

            if (taiKhoan == null || taiKhoan.Matkhauhash != HashPassword(model.Password) || taiKhoan.Trangthai != "Hoạt động")
            {
                return Unauthorized(new { success = false, message = "Tài khoản hoặc mật khẩu không đúng." });
            }

            string fullName = taiKhoan.Tendangnhap ?? "User";
            if (taiKhoan.Vaitro == VaiTroHeThong.Customer)
            {
                var kh = await _context.Khachhangs.FirstOrDefaultAsync(k => k.Taikhoanid == taiKhoan.Taikhoanid);
                if (kh != null) fullName = kh.Hoten ?? fullName;
            }
            else
            {
                var nv = await _context.Nhanviens.FirstOrDefaultAsync(n => n.Taikhoanid == taiKhoan.Taikhoanid);
                if (nv != null) fullName = nv.Hoten ?? fullName;
            }

            return Json(new { success = true, user = new { username = taiKhoan.Tendangnhap, fullName, role = taiKhoan.Vaitro.ToString() } });
        }

        [HttpPost("CheckUsername")]
        public async Task<IActionResult> CheckUsername([FromBody] CheckUsernameViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Username)) return BadRequest(new { success = false });
            string usernameInput = model.Username.ToUpper();

            var exists = await _context.Taikhoans.AnyAsync(t => (t.Tendangnhap ?? "").ToUpper() == usernameInput || (t.Email != null && t.Email.ToUpper() == usernameInput));
            if (!exists) exists = await _context.Khachhangs.AnyAsync(k => k.Sodienthoai == model.Username);

            if (!exists) return Json(new { success = false, message = "Tài khoản không tồn tại." });
            return Json(new { success = true });
        }

        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            if (!ModelState.IsValid) return BadRequest(new { success = false });
            string usernameInput = (model.Username ?? "").ToUpper();

            var taiKhoan = await _context.Taikhoans.FirstOrDefaultAsync(t => (t.Tendangnhap ?? "").ToUpper() == usernameInput || (t.Email != null && t.Email.ToUpper() == usernameInput));
            if (taiKhoan == null)
            {
                var khachHang = await _context.Khachhangs.Include(k => k.Taikhoan).FirstOrDefaultAsync(k => k.Sodienthoai == model.Username);
                if (khachHang != null) taiKhoan = khachHang.Taikhoan;
            }

            if (taiKhoan == null) return NotFound(new { success = false, message = "Không tìm thấy tài khoản." });

            taiKhoan.Matkhauhash = HashPassword(model.NewPassword);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đổi mật khẩu thành công!" });
        }

        [HttpGet("GetUserInfo")]
        public async Task<IActionResult> GetUserInfo([FromQuery] string username)
        {
            if (string.IsNullOrEmpty(username)) return BadRequest(new { success = false });

            // Fix CS8602: Thêm check k.Taikhoan != null
            var khachHang = await _context.Khachhangs.Include(k => k.Taikhoan)
                .FirstOrDefaultAsync(k => k.Taikhoan != null && (k.Taikhoan.Tendangnhap ?? "").ToUpper() == username.ToUpper());

            if (khachHang == null)
            {
                var nhanVien = await _context.Nhanviens.Include(n => n.Taikhoan)
                    .FirstOrDefaultAsync(n => n.Taikhoan != null && (n.Taikhoan.Tendangnhap ?? "").ToUpper() == username.ToUpper());
                if (nhanVien != null) return Json(new { fullName = nhanVien.Hoten, email = nhanVien.Taikhoan?.Email, phone = nhanVien.Sodienthoai, address = "N/A" });
                return NotFound(new { success = false, message = "Không tìm thấy thông tin." });
            }
            return Json(new { fullName = khachHang.Hoten, email = khachHang.Email, phone = khachHang.Sodienthoai, address = khachHang.Diachi });
        }

        [HttpPost("UpdateUserInfo")]
        public async Task<IActionResult> UpdateUserInfo([FromBody] UpdateInfoViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(new { success = false });
            // Fix CS8602
            var khachHang = await _context.Khachhangs.Include(k => k.Taikhoan)
                .FirstOrDefaultAsync(k => k.Taikhoan != null && (k.Taikhoan.Tendangnhap ?? "").ToUpper() == (model.Username ?? "").ToUpper());

            if (khachHang == null) return NotFound(new { success = false });
            var taiKhoan = khachHang.Taikhoan;

            // Check thêm cho chắc chắn
            if (taiKhoan == null) return NotFound(new { success = false });

            if (taiKhoan.Tendangnhap != model.Phone)
            {
                if (await _context.Taikhoans.AnyAsync(t => (t.Tendangnhap ?? "").ToUpper() == (model.Phone ?? "").ToUpper()))
                    return Conflict(new { success = false, message = "Số điện thoại mới đã tồn tại." });
                taiKhoan.Tendangnhap = model.Phone;
            }
            khachHang.Hoten = model.FullName;
            khachHang.Email = model.Email;
            khachHang.Sodienthoai = model.Phone;
            khachHang.Diachi = model.Address;
            if (taiKhoan.Email != model.Email) taiKhoan.Email = model.Email;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cập nhật thành công!", newFullName = khachHang.Hoten, newUsername = taiKhoan.Tendangnhap });
        }

        [HttpGet("GetHistoryData")]
        public async Task<IActionResult> GetHistoryData([FromQuery] string username, [FromQuery] string status)
        {
            if (string.IsNullOrEmpty(username)) return BadRequest(new { success = false });
            var taiKhoan = await _context.Taikhoans.FirstOrDefaultAsync(t => (t.Tendangnhap ?? "").ToUpper() == username.ToUpper());
            if (taiKhoan == null) return NotFound(new { success = false });

            IQueryable<Datban> query = _context.Datbans
                .Include(d => d.Banphong).Include(d => d.Khachhang).Include(d => d.Hoadons).ThenInclude(h => h.Trangthai);

            if (taiKhoan.Vaitro == VaiTroHeThong.Customer)
            {
                var khachHang = await _context.Khachhangs.FirstOrDefaultAsync(k => k.Taikhoanid == taiKhoan.Taikhoanid);
                if (khachHang != null) query = query.Where(d => d.Khachhangid == khachHang.Khachhangid);
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

            // Fix CS8629: Dùng .GetValueOrDefault() hoặc ?? 0 thay vì .Value trực tiếp
            if (filterHuy)
                query = query.Where(d => (d.Trangthai ?? "").ToLower() == "đã hủy" || d.Hoadons.Any(h => h.Trangthaiid == (trangThaiId ?? 5)));
            else if (trangThaiId.HasValue)
                query = query.Where(d => d.Hoadons.Any(h => h.Trangthaiid == trangThaiId.GetValueOrDefault()));
            else if (filterCho)
                query = query.Where(d => (d.Trangthai ?? "").ToLower() == "chờ xác nhận");

            var list = await query.OrderByDescending(d => d.Ngayden)
                .Select(d => new {
                    datBanId = d.Datbanid,
                    ngayDen = d.Ngayden.ToString("dd/MM/yyyy"),
                    tenKhachHang = d.Khachhang != null ? d.Khachhang.Hoten : "Ẩn danh",
                    tenBanPhong = d.Banphong != null ? d.Banphong.Tenbanphong : "N/A",
                    soNguoi = d.Songuoi,
                    trangThaiDatBan = d.Trangthai,
                    trangThaiHoaDon = d.Hoadons.OrderByDescending(h => h.Ngaylap).Select(h => h.Trangthai.Tentrangthai).FirstOrDefault()
                }).ToListAsync();
            return Json(new { success = true, list });
        }

        [HttpGet("GetUserVouchers")]
        public async Task<IActionResult> GetUserVouchers([FromQuery] string username)
        {
            if (string.IsNullOrEmpty(username)) return BadRequest(new { success = false });
            var taiKhoan = await _context.Taikhoans.FirstOrDefaultAsync(t => (t.Tendangnhap ?? "").ToUpper() == username.ToUpper());
            if (taiKhoan == null) return NotFound(new { success = false, message = "Tài khoản lỗi." });

            var khachHang = await _context.Khachhangs.Include(k => k.Hangthanhvien).FirstOrDefaultAsync(k => k.Taikhoanid == taiKhoan.Taikhoanid);
            if (khachHang == null) return NotFound(new { success = false, message = "Không tìm thấy thông tin khách hàng." });

            string hangThanhVien = khachHang.Hangthanhvien?.Tenhang ?? "Thường";

            // Fix CS0019: Bỏ ?? 0 vì Diemtichluy là int (không null). 
            // Dùng Convert.ToInt32 để an toàn tuyệt đối.
            int diem = Convert.ToInt32(khachHang.Diemtichluy);

            var vouchers = new List<object>();
            string ex30 = DateTime.Now.AddDays(30).ToString("dd/MM/yyyy");
            string ex90 = DateTime.Now.AddMonths(3).ToString("dd/MM/yyyy");
            string ex365 = DateTime.Now.AddYears(1).ToString("dd/MM/yyyy");

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

            var khungGio = await _context.Khunggios.FirstOrDefaultAsync(k => k.Tenkhunggio.ToLower() == key);

            return khungGio != null ? (int)khungGio.Khunggioid : 0;
        }

        private string HashPassword(string password)
        {
            using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }

        [HttpGet("HistoryDetail/{datBanId}")]
        public async Task<IActionResult> HistoryDetail(int datBanId)
        {
            var datBan = await _context.Datbans
                .Include(d => d.Khunggio).Include(d => d.Banphong).Include(d => d.Khachhang)
                .Include(d => d.Hoadons).ThenInclude(h => h.Trangthai)
                .Include(d => d.Hoadons).ThenInclude(h => h.Chitiethoadons).ThenInclude(ct => ct.Monan)
                .FirstOrDefaultAsync(d => d.Datbanid == datBanId);

            if (datBan == null) return NotFound("Không tìm thấy đơn đặt bàn.");

            ViewBag.DanhSachBan = await _context.Banphongs.Include(b => b.Loaibanphong).OrderBy(b => b.Banphongid).ToListAsync();
            ViewBag.DanhSachMonAn = await _context.Monans.Where(m => m.Trangthai == "Còn bán").Include(m => m.Danhmuc).OrderBy(m => m.Danhmucid).ToListAsync();
            return View(datBan);
        }

        [HttpPost("UpdateBooking")]
        public async Task<IActionResult> UpdateBooking([FromBody] UpdateBookingViewModel model)
        {
            if (model == null)
            {
                return BadRequest(new { success = false, message = "Lỗi: Server không đọc được dữ liệu gửi lên (JSON Null)." });
            }

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                                            .SelectMany(x => x.Errors)
                                            .Select(x => x.ErrorMessage));
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ: " + errors });
            }

            // 3. Logic xử lý chính (Giữ nguyên logic cũ)
            var datBan = await _context.Datbans
                .Include(d => d.Hoadons).ThenInclude(h => h.Chitiethoadons)
                .FirstOrDefaultAsync(d => d.Datbanid == model.DatBanId);

            if (datBan == null) return NotFound(new { success = false, message = "Không tìm thấy đơn đặt bàn." });

            var hoaDon = datBan.Hoadons.FirstOrDefault();
            if ((datBan.Trangthai ?? "").ToLower() != "chờ xác nhận" && (hoaDon != null && hoaDon.Trangthaiid != 1))
                return BadRequest(new { success = false, message = "Không thể sửa đơn này do trạng thái không hợp lệ." });

            if (!DateOnly.TryParse(model.BookingDate, out DateOnly bookingDate))
                return BadRequest(new { success = false, message = "Ngày đặt không hợp lệ." });

            int khungGioId = await ResolveKhungGioId(model.TimeSlot);
            if (khungGioId == 0) return BadRequest(new { success = false, message = "Khung giờ lỗi." });

            if (model.BanPhongId.HasValue && model.BanPhongId != datBan.Banphongid)
            {
                var banMoi = await _context.Banphongs.FindAsync(model.BanPhongId.GetValueOrDefault());
                if (banMoi == null || banMoi.Trangthai)
                    return BadRequest(new { success = false, message = "Bàn đã chọn không còn trống." });
            }

            // 4. Lưu dữ liệu
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    datBan.Ngayden = bookingDate.ToDateTime(TimeOnly.MinValue);
                    datBan.Khunggioid = khungGioId;
                    datBan.Songuoi = model.GuestCount;
                    datBan.Banphongid = model.BanPhongId;

                    if (hoaDon != null)
                    {
                        _context.Chitiethoadons.RemoveRange(hoaDon.Chitiethoadons);
                        await _context.SaveChangesAsync();

                        decimal newTotal = 0;
                        if (model.Items != null)
                        {
                            foreach (var item in model.Items)
                            {
                                var thanhTien = item.DonGia * item.SoLuong;
                                _context.Chitiethoadons.Add(new Chitiethoadon
                                {
                                    Hoadonid = (int)hoaDon.Hoadonid,
                                    Monanid = (int)item.MonAnId,
                                    Soluong = item.SoLuong,
                                    Dongia = (long)item.DonGia,
                                    Thanhtien = (long)thanhTien
                                });
                                newTotal += thanhTien;
                            }
                        }
                        hoaDon.Tongtien = (long)newTotal;
                        hoaDon.Banphongid = model.BanPhongId;
                        datBan.Tongtiendukien = (long?)newTotal;
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
            var taiKhoan = await _context.Taikhoans.FirstOrDefaultAsync(t => (t.Tendangnhap ?? "").ToUpper() == (req.Username ?? "").ToUpper());
            if (taiKhoan == null) return Unauthorized(new { success = false });

            var khachHang = await _context.Khachhangs.FirstOrDefaultAsync(k => k.Taikhoanid == taiKhoan.Taikhoanid);
            if (khachHang == null) return Unauthorized(new { success = false });

            var datBan = await _context.Datbans.Include(d => d.Hoadons).FirstOrDefaultAsync(d => d.Datbanid == req.DatBanId && d.Khachhangid == khachHang.Khachhangid);
            if (datBan == null) return NotFound(new { success = false });

            if ((datBan.Trangthai ?? "").ToLower() != "chờ xác nhận") return BadRequest(new { success = false, message = "Không thể hủy." });

            datBan.Trangthai = "Đã hủy";
            var hoaDon = datBan.Hoadons.FirstOrDefault();
            if (hoaDon != null)
            {
                hoaDon.Trangthaiid = 5;
                _context.Hoadons.Update(hoaDon);
            }
            _context.Datbans.Update(datBan);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã hủy đơn." });
        }
    }
}