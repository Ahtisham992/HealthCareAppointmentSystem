using Microsoft.AspNetCore.Mvc;

namespace HealthCareAppointmentSystem.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
