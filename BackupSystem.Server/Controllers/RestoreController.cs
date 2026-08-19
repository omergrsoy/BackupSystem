using BackupSystem.Server.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace BackupSystem.Server.Controllers
{
    [Authorize]
    public class RestoreController : Controller
    {
        private readonly AppDbContext _context;
        public RestoreController(AppDbContext context) { _context = context; }

        public IActionResult Index()
        {
            // Yedekler ve makineleri
            var backups = _context.Backups
                .Include(b => b.Machine)
                .OrderByDescending(b => b.BackupDate)
                .ToList();

            return View(backups);
        }

        [HttpPost]
        public IActionResult StartRestore(int backupId)
        {
            // SAHTE
            TempData["Message"] = "Geri yükleme emri sıraya alındı! Ajan (Agent) bir sonraki sinyalinde şifreli dosyayı çekip kurtarma işlemini başlatacak.";
            return RedirectToAction("Index");
        }
    }
}