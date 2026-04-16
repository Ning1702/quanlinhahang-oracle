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

            // Sửa Danhmucmons -> DanhMucMons, Danhmucid -> DanhMucId
            var danhMucList = await _db.DanhMucMons.OrderBy(dm => dm.DanhMucId).ToListAsync();

            vm.DanhMucList = danhMucList
                .GroupBy(d => d.TenDanhMuc.Trim()) // Sửa Tendanhmuc -> TenDanhMuc
                .Select(g => g.First())
                .ToList();

            // Sửa Monans -> MonAns
            var q = _db.MonAns.AsQueryable();

            if (danhMucId.HasValue && danhMucId.Value > 0)
            {
                q = q.Where(m => m.DanhMucId == danhMucId.Value);
                vm.DanhMucId = danhMucId;
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToUpper();

                // Sửa Tenmon -> TenMon, Mota -> MoTa
                q = q.Where(m => m.TenMon.ToUpper().Contains(s)
                              || (m.MoTa != null && m.MoTa.ToUpper().Contains(s)));

                vm.Search = search;
            }

            // Sửa Monanid -> MonAnId
            vm.MonAnList = await q.OrderBy(m => m.MonAnId).ToListAsync();

            return View(vm);
        }

        [HttpPost]
        public IActionResult SaveCart([FromBody] List<CartItem> cart)
        {
            if (cart == null || !cart.Any())
            {
                return BadRequest("Giỏ hàng trống");
            }

            HttpContext.Session.SetString("CartData", JsonConvert.SerializeObject(cart));

            return Ok(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> DatBan()
        {
            // Sửa Banphongs -> BanPhongs, Loaibanphong -> LoaiBanPhong, Banphongid -> BanPhongId
            var danhSachBan = await _db.BanPhongs
                                     .Include(b => b.LoaiBanPhong)
                                     .OrderBy(b => b.BanPhongId)
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