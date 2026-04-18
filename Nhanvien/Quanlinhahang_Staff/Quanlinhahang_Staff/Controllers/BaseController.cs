using Microsoft.AspNetCore.Mvc;

namespace Quanlinhahang_Staff.Controllers
{
    public class BaseController : Controller
    {
        protected int? CurrentUserId
            => HttpContext.Session.GetInt32("UserId");

        protected bool IsLoggedIn
            => CurrentUserId != null;

        protected IActionResult RequireLogin()
        {
            return Redirect("https://quanlinhahang-admin.onrender.com/Account/Login");
        }
    }
}