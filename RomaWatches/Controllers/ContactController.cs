using Microsoft.AspNetCore.Mvc;

namespace RomaWatches.Controllers
{
    // Controller quản lý trang liên hệ.
    public class ContactController : Controller
    {
        // Action hiển thị trang liên hệ chính.
        // GET: /Contact
        public IActionResult Index()
        {
            return View(); // Trả về view Index.cshtml tương ứng.
        }
    }
}
