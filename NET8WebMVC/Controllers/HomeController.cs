using Microsoft.AspNetCore.Mvc;
using NET8WebMVC.DB;
using NET8WebMVC.Domain.Repositories;
using NET8WebMVC.Models;
using System.Diagnostics;

namespace NET8WebMVC.Controllers {
    public class HomeController : Controller {
        private readonly SampleDbContext _context;
        private readonly ILogger<HomeController> _logger;
        private readonly ILoaderMembers _loaderMembers;
        private readonly IConfiguration _config;

        public HomeController(ILogger<HomeController> logger, SampleDbContext context, ILoaderMembers loaderMembers, IConfiguration config) {
            _logger = logger;
            _context = context;
            _loaderMembers = loaderMembers;
            _config = config;
        }

        public IActionResult Index() {
            return View();
        }

        public IActionResult Privacy() {
            ViewBag.MySetting = _config["MySetting"];
            return View();
        }

        public IActionResult Sample(int? id) {
            var member = _context.Members.FirstOrDefault();     // DBコンテキストだけDIなデータ読み出し
            if (id != null) {
            member =_context.Members.FirstOrDefault(r => r.Id == id);
            }
            return View(member);
        }
        public IActionResult Samples() {
            _logger.LogInformation("LogInformation.");
            var members = _loaderMembers.Load();        // 依存性注入済みのデータ読み出し
            return View(members);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() {
            _logger.LogCritical("LogCritical!!");
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
