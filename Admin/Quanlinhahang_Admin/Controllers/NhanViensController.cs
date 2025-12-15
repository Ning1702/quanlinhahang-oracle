using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quanlinhahang.Data.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

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
            // [SỬA]: Nhanviens, Taikhoan
            var query = _context.Nhanviens
                .Include(n => n.Taikhoan)
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
                            // [SỬA]: Nhanvienid
                            query = query.Where(x => x.Nhanvienid == idValue);
                            if (!query.Any()) ViewBag.NotFound = "Không tìm thấy nhân viên!";
                        }
                        else ViewBag.EmptyError = "ID phải là số!";
                        break;

                    case "phone":
                        // [SỬA]: Sodienthoai
                        query = query.Where(x => x.Sodienthoai != null && x.Sodienthoai.Contains(keyword));
                        if (!query.Any()) ViewBag.NotFound = "Không tìm thấy nhân viên!";
                        break;
                }
            }

            return View(await query.ToListAsync());
        }

        // ================== CREATE (GET) ==================
        // ================== CREATE (GET) ==================
        public IActionResult Create()
        {
            // [Lưu ý]: Không cần tính NextID vì Oracle tự tăng, nhưng nếu muốn hiển thị thì ok
            int nextId = 1;
            if (_context.Nhanviens.Any())
                nextId = _context.Nhanviens.Max(n => n.Nhanvienid) + 1;

            ViewBag.NextID = nextId;
            return View();
        }

        // ================== CREATE (POST) ==================
        [HttpPost]
        public async Task<IActionResult> Create(Nhanvien nv, string TenDangNhap, string MatKhau, string VaiTro)
        {
            if (await _context.Nhanviens.AnyAsync(x => x.Sodienthoai == nv.Sodienthoai))
            {
                ViewBag.PhoneError = "Số điện thoại đã tồn tại!";
                return View(nv);
            }

            if (await _context.Taikhoans.AnyAsync(t => t.Tendangnhap == TenDangNhap))
            {
                ViewBag.UsernameError = "Tên đăng nhập đã tồn tại!";
                return View(nv);
            }

            var acc = new Taikhoan
            {
                Tendangnhap = TenDangNhap,
                Matkhauhash = MatKhau,
                Email = $"{TenDangNhap}@quanlynhahang.vn",
                Vaitro = (VaiTro == "Admin") ? VaiTroHeThong.Admin : VaiTroHeThong.Staff,
                Trangthai = "Hoạt động"
            };

            _context.Taikhoans.Add(acc);
            await _context.SaveChangesAsync();

            nv.Taikhoanid = acc.Taikhoanid;
            _context.Nhanviens.Add(nv);
            await _context.SaveChangesAsync();

            TempData["msg"] = "Thêm nhân viên thành công!";
            return RedirectToAction(nameof(Index));
        }


        // ================== EDIT (GET) ==================
        public async Task<IActionResult> Edit(int id, string? returnUrl)
        {
            var nv = await _context.Nhanviens
                .Include(x => x.Taikhoan)
                .FirstOrDefaultAsync(x => x.Nhanvienid == id);

            if (nv == null) return NotFound();

            ViewBag.ReturnUrl = returnUrl;
            return View(nv);
        }

        public async Task<IActionResult> Edit(int id, Nhanvien nv, string TenDangNhap, string MatKhau, string VaiTro, string? returnUrl)
        {
            if (id != nv.Nhanvienid) return NotFound();

            var existingNV = await _context.Nhanviens.Include(n => n.Taikhoan).FirstOrDefaultAsync(n => n.Nhanvienid == id);
            if (existingNV == null) return NotFound();

            if (await _context.Nhanviens.AnyAsync(x => x.Sodienthoai == nv.Sodienthoai && x.Nhanvienid != id))
            {
                ViewBag.PhoneError = "Số điện thoại đã tồn tại!";
                return View(nv);
            }

            existingNV.Hoten = nv.Hoten;
            existingNV.Sodienthoai = nv.Sodienthoai;
            existingNV.Chucvu = nv.Chucvu;
            existingNV.Ngayvaolam = nv.Ngayvaolam;
            existingNV.Trangthai = nv.Trangthai;
            if (existingNV.Taikhoan != null)
            {
                if (existingNV.Taikhoan.Tendangnhap != TenDangNhap)
                {
                    if (await _context.Taikhoans.AnyAsync(t => t.Tendangnhap == TenDangNhap))
                    {
                        ViewBag.UsernameError = "Tên đăng nhập đã tồn tại!";
                        return View(nv);
                    }
                    existingNV.Taikhoan.Tendangnhap = TenDangNhap;
                }

                existingNV.Taikhoan.Matkhauhash = MatKhau;
                existingNV.Taikhoan.Vaitro = (VaiTro == "Admin") ? VaiTroHeThong.Admin : VaiTroHeThong.Staff;
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
            var nv = await _context.Nhanviens.FindAsync(id);
            if (nv == null) return NotFound();

            _context.Nhanviens.Remove(nv);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa nhân viên!" });
        }
    }
}
