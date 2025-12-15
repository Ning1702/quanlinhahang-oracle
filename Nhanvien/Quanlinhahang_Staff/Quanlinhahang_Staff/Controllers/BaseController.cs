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
            // PORT ADMIN: 7011 (Dựa trên ảnh lỗi localhost:7011 của bạn)
            return Redirect("https://localhost:7011/Account/Login");
        }
    }
}