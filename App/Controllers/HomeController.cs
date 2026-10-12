using Microsoft.AspNetCore.Mvc;
using App.Models;
using System.Diagnostics;

namespace App.Controllers {
    public class HomeController : Controller {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _config;

        public HomeController(ILogger<HomeController> logger, IConfiguration config) {
            _logger = logger;
            _config = config;
        }

        public IActionResult Index() {
            return View();
        }

        public IActionResult Privacy() {
            ViewBag.MySetting = _config["MySetting"];
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() {
            _logger.LogCritical("LogCritical!!");
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
