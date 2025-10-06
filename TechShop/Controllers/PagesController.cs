using Microsoft.AspNetCore.Mvc;

namespace TechShop.Controllers
{
    public class PagesController : Controller
    {
        public IActionResult AboutUs()
        {
            return View();
        }

        public IActionResult Contacts()
        {
            return View();
        }
    }
}
