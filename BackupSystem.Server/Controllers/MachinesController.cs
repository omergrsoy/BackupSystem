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

        // 4. DÜZENLEME EKRANINI GETİRME
        public IActionResult Edit(int id)
        {
            var machine = _context.Machines.Find(id);
            if (machine == null)
            {
                return NotFound();
            }
            return View(machine);
        }

        // 5. DÜZENLEME İŞLEMİNİ KAYDETME
        [HttpPost]
        public IActionResult Edit(int id, Machine machine)
        {
            if (id != machine.Id)
            {
                return NotFound();
            }

            ModelState.Remove("Backups"); // Yine Backups listesini doğrulamadan çıkarıyoruz

            if (ModelState.IsValid)
            {
                _context.Machines.Update(machine);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(machine);
        }

        // 6. SİLME İŞLEMİ (Doğrudan veritabanından siler ve listeye döner)
        public IActionResult Delete(int id)
        {
            var machine = _context.Machines.Find(id);
            if (machine != null)
            {
                _context.Machines.Remove(machine);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }

        // 7. MANUEL YEDEKLEME TETİKLEYİCİ
        public IActionResult RequestBackup(int id, string type)
        {
            var machine = _context.Machines.Find(id);
            if (machine != null)
            {
                machine.IsBackupRequested = true;
                machine.RequestedBackupType = type; // Seçilen tipi (Tam, Artımlı, Fark) kaydediyoruz
                _context.SaveChanges();
                TempData["Message"] = $"{machine.Name} makinesi için '{type}' yedekleme emri kuyruğa alındı.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
    
}
