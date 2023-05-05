using Microsoft.AspNetCore.Mvc;

namespace RTDWebAPI.Controllers
{
    public class CorsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
