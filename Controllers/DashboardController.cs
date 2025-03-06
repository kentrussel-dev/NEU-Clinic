using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        public IActionResult Index(string activeTab = "home")
        {
            ViewBag.ActiveTab = activeTab;
            return View("Dashboard");
        }
    }
}
