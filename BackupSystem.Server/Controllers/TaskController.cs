using Microsoft.AspNetCore.Mvc;

namespace BackupSystem.Server.Controllers
{
    public class TaskController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
