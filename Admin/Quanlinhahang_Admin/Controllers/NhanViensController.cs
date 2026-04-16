using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quanlinhahang.Data.Models;

namespace Quanlinhahang_Admin.Controllers
{
    public class NhanViensController : Controller
    {
        private readonly QuanLyNhaHangContext _context;

        public NhanViensController(QuanLyNhaHangContext context)
        {
            _context = context;
        }

        // ================== INDEX ==================
        public async Task<IActionResult> Index(string? searchType, string? keyword)
        {
            var query = _context.NhanViens
                .Include(n => n.TaiKhoan)
                .AsQueryable();

            ViewBag.SearchType = searchType ?? "";
            ViewBag.Keyword = keyword ?? "";
            ViewBag.EmptyError = "";
            ViewBag.NotFound = "";

            if (!string.IsNullOrEmpty(searchType))
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    ViewBag.EmptyError = "Vui lòng nhập dữ liệu tìm kiếm!";
                    return View(await query.ToListAsync());
                }

                switch (searchType)
                {
                    case "id":
                        if (int.TryParse(keyword, out int idValue))
                        {
                            query = query.Where(x => x.NhanVienId == idValue);
                            if (!query.Any()) ViewBag.NotFound = "Không tìm thấy nhân viên!";
                        }
                        else ViewBag.EmptyError = "ID phải là số!";
                        break;

                    case "phone":
                        query = query.Where(x => x.SoDienThoai != null && x.SoDienThoai.Contains(keyword));
                        if (!query.Any()) ViewBag.NotFound = "Không tìm thấy nhân viên!";
                        break;
                }
            }

            return View(await query.ToListAsync());
        }

        // ================== CREATE (GET) ==================
        public IActionResult Create()
        {
            // Tự động tính ID tiếp theo để hiển thị (nếu cần)
            int nextId = 1;
            if (_context.NhanViens.Any())
                nextId = _context.NhanViens.Max(n => n.NhanVienId) + 1;

            ViewBag.NextID = nextId;
            return View();
        }

        // ================== CREATE (POST) ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NhanVien nv, string TenDangNhap, string MatKhau, string VaiTro)
        {
            if (await _context.NhanViens.AnyAsync(x => x.SoDienThoai == nv.SoDienThoai))
            {
                ViewBag.PhoneError = "Số điện thoại đã tồn tại!";
                return View(nv);
            }

            if (await _context.TaiKhoans.AnyAsync(t => t.TenDangNhap == TenDangNhap))
            {
                ViewBag.UsernameError = "Tên đăng nhập đã tồn tại!";
                return View(nv);
            }

            var acc = new TaiKhoan
            {
                TenDangNhap = TenDangNhap,
                MatKhauHash = MatKhau,
                Email = $"{TenDangNhap}@quanlynhahang.vn",
                VaiTro = (VaiTro == "Admin") ? "Admin" : "Staff",
                TrangThai = "Hoạt động" // Cột này vẫn nằm trong bảng TaiKhoan
            };

            _context.TaiKhoans.Add(acc);
            await _context.SaveChangesAsync();

            nv.TaiKhoanId = acc.TaiKhoanId;
            _context.NhanViens.Add(nv);
            await _context.SaveChangesAsync();

            TempData["msg"] = "Thêm nhân viên thành công!";
            return RedirectToAction(nameof(Index));
        }

        // ================== EDIT (GET) ==================
        public async Task<IActionResult> Edit(int id, string? returnUrl)
        {
            var nv = await _context.NhanViens
                .Include(x => x.TaiKhoan)
                .FirstOrDefaultAsync(x => x.NhanVienId == id);

            if (nv == null) return NotFound();

            ViewBag.ReturnUrl = returnUrl;
            return View(nv);
        }

        // ================== EDIT (POST) ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NhanVien nv, string TenDangNhap, string MatKhau, string VaiTro, string? returnUrl)
        {
            if (id != nv.NhanVienId) return NotFound();

            var existingNV = await _context.NhanViens.Include(n => n.TaiKhoan).FirstOrDefaultAsync(n => n.NhanVienId == id);
            if (existingNV == null) return NotFound();

            if (await _context.NhanViens.AnyAsync(x => x.SoDienThoai == nv.SoDienThoai && x.NhanVienId != id))
            {
                ViewBag.PhoneError = "Số điện thoại đã tồn tại!";
                return View(nv);
            }

            existingNV.HoTen = nv.HoTen;
            existingNV.SoDienThoai = nv.SoDienThoai;
            existingNV.ChucVu = nv.ChucVu;
            // LƯU Ý: Đã xóa 2 dòng NgayVaoLam và TrangThai vì bảng NhanVien mới không có 2 cột này

            if (existingNV.TaiKhoan != null)
            {
                if (existingNV.TaiKhoan.TenDangNhap != TenDangNhap)
                {
                    if (await _context.TaiKhoans.AnyAsync(t => t.TenDangNhap == TenDangNhap))
                    {
                        ViewBag.UsernameError = "Tên đăng nhập đã tồn tại!";
                        return View(nv);
                    }
                    existingNV.TaiKhoan.TenDangNhap = TenDangNhap;
                }

                existingNV.TaiKhoan.MatKhauHash = MatKhau;
                existingNV.TaiKhoan.VaiTro = (VaiTro == "Admin") ? "Admin" : "Staff";
            }

            await _context.SaveChangesAsync();
            TempData["msg"] = "Cập nhật thành công!";

            if (!string.IsNullOrEmpty(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction(nameof(Index));
        }

        // ================== DELETE ==================
        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var nv = await _context.NhanViens.FindAsync(id);
            if (nv == null) return NotFound();

            try
            {
                _context.NhanViens.Remove(nv);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Đã xóa nhân viên!" });
            }
            catch
            {
                return BadRequest();
            }
        }
    }
}