using Microsoft.AspNetCore.Mvc;

namespace RomaWatches.Controllers
{
    public class AboutController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

