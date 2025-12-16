using Microsoft.AspNetCore.Mvc;

namespace RomaWatches.Controllers
{
    // Controller quản lý các trang hỗ trợ khách hàng.
    public class SupportController : Controller
    {
        // Action hiển thị trang hướng dẫn mua hàng.
        // GET: /Support
        public IActionResult Index()
        {
            return View(); // Trả về view Index.cshtml (Hướng dẫn mua hàng).
        }

        // Action hiển thị trang hướng dẫn thanh toán.
        // GET: /Support/Payment
        public IActionResult Payment()
        {
            return View(); // Trả về view Payment.cshtml.
        }

        // Action hiển thị trang chính sách vận chuyển.
        // GET: /Support/Shipping
        public IActionResult Shipping()
        {
            return View(); // Trả về view Shipping.cshtml.
        }

        // Action hiển thị trang chính sách bảo hành.
        // GET: /Support/Warranty
        public IActionResult Warranty()
        {
            return View(); // Trả về view Warranty.cshtml.
        }
    }
}
