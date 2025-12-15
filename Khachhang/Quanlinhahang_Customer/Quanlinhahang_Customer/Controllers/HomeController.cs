using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Quanlinhahang.Data.Models;
using System.Diagnostics;
using Quanlinhahang_Customer.Models.ViewModels;

namespace Quanlinhahang_Customer.Controllers
{
    public class HomeController : Controller
    {
        private readonly QuanLyNhaHangContext _db;

        public HomeController(QuanLyNhaHangContext db)
        {
            _db = db;
        }

        public ActionResult Index()
        {
            return RedirectToAction("GioiThieu");
        }

        public ActionResult GioiThieu()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Menu(string search, int? danhMucId)
        {
            var vm = new MenuViewModel();

            // 1. LOAD DANH MỤC
            var danhMucList = await _db.Danhmucmons.OrderBy(dm => dm.Danhmucid).ToListAsync();

            // Xử lý trùng tên (nếu cần)
            vm.DanhMucList = danhMucList
                .GroupBy(d => d.Tendanhmuc.Trim())
                .Select(g => g.First())
                .ToList();

            // 2. TRUY VẤN MÓN ĂN
            var q = _db.Monans.AsQueryable();

            // Lọc theo danh mục
            if (danhMucId.HasValue && danhMucId.Value > 0)
            {
                q = q.Where(m => m.Danhmucid == danhMucId.Value);
                vm.DanhMucId = danhMucId;
            }

            // Tìm kiếm
            if (!string.IsNullOrWhiteSpace(search))
            {
                // [ORACLE FIX]: Chuyển cả từ khóa và dữ liệu trong DB về chữ hoa để so sánh
                // Oracle phân biệt hoa thường rất chặt (ví dụ: tìm "ga" sẽ không ra "Gà" nếu không xử lý)
                var s = search.Trim().ToUpper();

                q = q.Where(m => m.Tenmon.ToUpper().Contains(s)
                              || (m.Mota != null && m.Mota.ToUpper().Contains(s)));

                vm.Search = search;
            }

            // Chỉ lấy món đang còn bán (Tùy chọn)
            // q = q.Where(m => m.TrangThai == "Còn bán");

            vm.MonAnList = await q.OrderBy(m => m.Monanid).ToListAsync();

            return View(vm);
        }

        // API lưu giỏ hàng vào Session
        [HttpPost]
        public IActionResult SaveCart([FromBody] List<CartItem> cart)
        {
            if (cart == null || !cart.Any())
            {
                return BadRequest("Giỏ hàng trống");
            }

            // Lưu danh sách CartItem vào Session dưới dạng chuỗi JSON
            HttpContext.Session.SetString("CartData", JsonConvert.SerializeObject(cart));

            return Ok(new { success = true });
        }

        // Action Đặt bàn: Tải danh sách bàn cho sơ đồ
        [HttpGet]
        public async Task<IActionResult> DatBan()
        {
            var danhSachBan = await _db.Banphongs
                                     .Include(b => b.Loaibanphong)
                                     .OrderBy(b => b.Banphongid)
                                     .ToListAsync();

            var viewModel = new DatBanViewModel
            {
                DanhSachBan = danhSachBan
            };

            return View(viewModel);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}