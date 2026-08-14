using BackupSystem.Server.Data;
using BackupSystem.Server.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace BackupSystem.Server.Controllers
{
    public class MachinesController : Controller
    {
        private readonly AppDbContext _context;

        public MachinesController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var machines = _context.Machines.ToList();
            return View(machines);
        }
        public IActionResult Create() 
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Machine machine)
        {
            if (ModelState.IsValid)
            {
                // İlk eklemede varsayılan olarak şu anki zamanı atıyoruz
                machine.LastHeartbeat = DateTime.Now;

                _context.Machines.Add(machine);
                _context.SaveChanges();

                // Başarıyla eklendikten sonra listeleme (Index) sayfasına yönlendiriyoruz
                return RedirectToAction(nameof(Index));
            }
            return View(machine);
        }
    }
}
