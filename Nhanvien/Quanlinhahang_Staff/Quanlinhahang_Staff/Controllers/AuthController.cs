using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Quanlinhahang_Staff.Controllers
{
    public class AuthController : Controller
    {
        [HttpGet]
        public IActionResult FromAdmin(int userId)
        {
            HttpContext.Session.SetInt32("UserId", userId);

            return RedirectToAction("Index", "Invoices");
        }
    }
}