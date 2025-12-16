using Microsoft.AspNetCore.Mvc;

namespace RomaWatches.Controllers
{
    // Controller quản lý trang tin tức.
    public class NewsController : Controller
    {
        // Action hiển thị trang tin tức chính.
        // GET: /News
        public IActionResult Index()
        {
            return View(); // Trả về view Index.cshtml tương ứng.
        }
    }
}
