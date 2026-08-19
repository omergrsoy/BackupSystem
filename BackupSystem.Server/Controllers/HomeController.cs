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
            // İstatistikleri hesaplıyoruz
            ViewBag.TotalMachines = _context.Machines.Count();

            // 5 dakikadan yakın zamanda heartbeat atanlar çevrimiçi (aktif) sayılır
            var fiveMinsAgo = DateTime.Now.AddMinutes(-5);
            ViewBag.OnlineMachines = _context.Machines.Count(m => m.IsActive && m.LastHeartbeat >= fiveMinsAgo);

            ViewBag.TotalBackups = _context.Backups.Count();

            return View();
        }
    }
}