using BackupSystem.Server.Data;
using BackupSystem.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace BackupSystem.Server.Controllers
{
    [Authorize]
    public class LogsController : Controller
    {
        private readonly AppDbContext _context;

        public LogsController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Index()
        {
            // En yeniden en eskiye
            var logs = _context.SystemLogs.OrderByDescending(l => l.LogDate).ToList();
            return View(logs);
        }

        // HATA MERKEZİ
        public IActionResult Errors()
        {
            var errors = _context.SystemLogs
                .Where(l => l.LogLevel == "Error")
                .OrderByDescending(l => l.LogDate)
                .ToList();
            return View(errors);
        }

        // Hata Üretimi
        public IActionResult CreateTestError()
        {
            var testLog = new SystemLog
            {
                LogLevel = "Error",
                Message = "Simüle edilmiş test hatası oluşturuldu.",
                MachineName = "Test-PC",
                Details = "System.NullReferenceException: Object reference not set to an instance of an object.",
                LogDate = DateTime.Now.AddDays(-new Random().Next(0, 7)) // Son 7 gün içine rastgele dağıt
            };

            _context.SystemLogs.Add(testLog);
            _context.SaveChanges();

            TempData["Message"] = "Test hatası başarıyla fırlatıldı ve kaydedildi.";
            return RedirectToAction("Errors");
        }
    }
}