using BackupSystem.Server.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace BackupSystem.Server.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {

            ViewBag.TotalMachines = _context.Machines.Count();

            var offlineLimit = DateTime.Now.AddMinutes(-5);
            ViewBag.OnlineMachines = _context.Machines.Count(m => m.IsActive && m.LastHeartbeat >= offlineLimit);

            ViewBag.TotalBackups = _context.Backups.Count();

            string? driveLetter = Path.GetPathRoot(Directory.GetCurrentDirectory());
            DriveInfo drive = new DriveInfo(driveLetter);

            if (drive.IsReady)
            {
                double totalSpaceGB = drive.TotalSize / (1024.0 * 1024 * 1024);
                double freeSpaceGB = drive.TotalFreeSpace / (1024.0 * 1024 * 1024);
                double usedSpaceGB = totalSpaceGB - freeSpaceGB;
                double usedPercentage = (usedSpaceGB / totalSpaceGB) * 100;

                ViewBag.TotalSpace = Math.Round(totalSpaceGB, 1);
                ViewBag.UsedSpace = Math.Round(usedSpaceGB, 1);
                ViewBag.UsedPercentage = Math.Round(usedPercentage, 1);
            }
            else
            {
                ViewBag.TotalSpace = 0; ViewBag.UsedSpace = 0; ViewBag.UsedPercentage = 0;
            }

            // --- SON 7 GÜNÜN HATA GRAFİĞİ VERİSİ ---
            var last7Days = Enumerable.Range(0, 7).Select(i => DateTime.Now.Date.AddDays(-i)).Reverse().ToList();
            var errorCounts = new List<int>();

            foreach (var day in last7Days)
            {
                // O güne ait hataları say
                int count = _context.SystemLogs.Count(l => l.LogLevel == "Error" && l.LogDate.Date == day);
                errorCounts.Add(count);
            }

            ViewBag.ChartLabels = string.Join(",", last7Days.Select(d => $"'{d.ToString("dd MMM")}'"));
            ViewBag.ChartData = string.Join(",", errorCounts);

            return View();
        }

    }
}