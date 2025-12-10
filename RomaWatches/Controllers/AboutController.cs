using Microsoft.AspNetCore.Mvc;

namespace RomaWatches.Controllers
{
    // Controller quản lý trang giới thiệu.
    public class AboutController : Controller
    {
        // Action hiển thị trang giới thiệu chính.
        // GET: /About
        public IActionResult Index()
        {
            return View(); // Trả về view Index.cshtml tương ứng.
        }
    }
}

