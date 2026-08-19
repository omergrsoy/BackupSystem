using BackupSystem.Server.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace BackupSystem.Server.Controllers
{
    [Authorize]
    public class TasksController : Controller
    {
        private readonly AppDbContext _context;
        public TasksController(AppDbContext context) { _context = context; }

        public IActionResult Index()
        {
            // Sadece otomatik yedek saati ayarlanmış makineleri getir
            var scheduledMachines = _context.Machines
                .Where(m => m.DailyBackupTime != null)
                .OrderBy(m => m.DailyBackupTime)
                .ToList();

            return View(scheduledMachines);
        }
    }
}