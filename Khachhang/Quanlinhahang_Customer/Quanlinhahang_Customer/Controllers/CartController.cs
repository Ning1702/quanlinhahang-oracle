using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Quanlinhahang.Data.Models;
using Quanlinhahang_Customer.Models;
using Quanlinhahang_Customer.Models.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Quanlinhahang_Customer.Controllers
{
    public class CartController : Controller
    {
        private readonly QuanLyNhaHangContext _context;
        private const string SESSION_CART_KEY = "Cart";

        public CartController(QuanLyNhaHangContext context)
        {
            _context = context;
        }

        private List<CartItem> GetCartItems()
        {
            var session = HttpContext.Session.GetString(SESSION_CART_KEY);
            if (string.IsNullOrEmpty(session)) return new List<CartItem>();
            try
            {
                return JsonConvert.DeserializeObject<List<CartItem>>(session) ?? new List<CartItem>();
            }
            catch
            {
                return new List<CartItem>();
            }
        }

        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetString(SESSION_CART_KEY, JsonConvert.SerializeObject(cart));
        }

        // POST /Cart/AddToCart
        [HttpPost]
        public async Task<IActionResult> AddToCart([FromForm] int monAnId)
        {
            // [FIX CAST]: Tìm kiếm bằng decimal (vì DB là decimal)
            var mon = await _context.Monans.FindAsync((decimal)monAnId);

            if (mon == null) return NotFound(new { success = false, message = "Món không tồn tại" });

            var cart = GetCartItems();
            var item = cart.FirstOrDefault(c => c.MonAnId == monAnId);
            if (item == null)
            {
                cart.Add(new CartItem
                {
                    // [FIX ERROR 55]: Ép kiểu decimal sang int cho Model CartItem
                    MonAnId = (int)mon.Monanid,
                    TenMon = mon.Tenmon,
                    Gia = mon.Dongia,
                    SoLuong = 1
                });
            }
            else
            {
                item.SoLuong++;
            }

            SaveCart(cart);
            return Json(new { success = true, count = cart.Sum(c => c.SoLuong) });
        }

        [HttpGet]
        public IActionResult GetCart()
        {
            var cart = GetCartItems();
            return Json(new { success = true, items = cart });
        }

        [HttpPost]
        public IActionResult Clear()
        {
            SaveCart(new List<CartItem>());
            return Json(new { success = true });
        }
    }
}