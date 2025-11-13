using Microsoft.AspNetCore.Mvc;

namespace RomaWatches.Controllers
{
    public class ProductController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

