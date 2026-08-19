using BackupSystem.Server.Data;
using BackupSystem.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace BackupSystem.Server.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly AppDbContext _context;
        public SettingsController(AppDbContext context) { _context = context; }

        public IActionResult Index()
        {
            var settings = _context.SystemSettings.FirstOrDefault();
            if (settings == null)
            {
                settings = new SystemSetting { ChunkSizeMB = 10, MaxUploadSpeedMB = 5 };
                _context.SystemSettings.Add(settings);
                _context.SaveChanges();
            }
            return View(settings);
        }

        [HttpPost]
        public IActionResult SaveAgentSettings(int chunkSizeMB, int maxUploadSpeedMB)
        {
            var settings = _context.SystemSettings.FirstOrDefault();
            if (settings != null)
            {
                settings.ChunkSizeMB = chunkSizeMB;
                settings.MaxUploadSpeedMB = maxUploadSpeedMB;
                _context.SaveChanges();
                TempData["Message"] = "Agent yapılandırmaları başarıyla güncellendi! Değişiklikler bir sonraki sinyalde ajanlara iletilecektir.";
            }
            return RedirectToAction("Index");
        }
    }
}