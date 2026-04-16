using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quanlinhahang_Staff.Models; 
using System.Diagnostics;

namespace Quanlinhahang_Staff.Controllers
{ 
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (!IsLoggedIn) return RequireLogin();

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}