using BackupSystem.Server.Data;
using BackupSystem.Server.Models;
using BackupSystem.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BackupSystem.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AgentController : ControllerBase
    {
        private readonly AppDbContext _context;

        // Veritabanı bağlantısını içeri alıyoruz
        public AgentController(AppDbContext context)
        {
            _context = context;
        }

        // POST: /api/agent/heartbeat/{machineId}
        [HttpPost("heartbeat/{machineId}")]
        public IActionResult Heartbeat(int machineId)
        {
            var machine = _context.Machines.Find(machineId);
            if (machine == null) return NotFound();

            machine.LastHeartbeat = DateTime.Now;

            bool shouldBackup = machine.IsBackupRequested;
            string backupType = machine.RequestedBackupType;
            DateTime? referenceDate = null;

            // --- ZAMANLANMIŞ GÖREV KONTROLÜ (AUTOMATIC SCHEDULER) ---
            if (!shouldBackup && !string.IsNullOrEmpty(machine.DailyBackupTime))
            {
                var currentTimeStr = DateTime.Now.ToString("HH:mm");

                // Eğer saat geldiyse VE bugün bu otomatik yedek henüz çalışmadıysa
                if (currentTimeStr == machine.DailyBackupTime &&
                   (machine.LastScheduledBackupDate == null || machine.LastScheduledBackupDate.Value.Date < DateTime.Now.Date))
                {
                    shouldBackup = true;
                    backupType = "Artımlı"; // Zamanlanmış yedekler varsayılan olarak 'Artımlı' çalışır
                    machine.LastScheduledBackupDate = DateTime.Now; // Bugün çalıştı olarak işaretle

                    // Differential/Incremental referans tarihi hesaplama
                    var last = _context.Backups.Where(b => b.MachineId == machineId && b.Status == "Başarılı").OrderByDescending(b => b.BackupDate).FirstOrDefault();
                    referenceDate = last?.BackupDate;
                }
            }

            // Manuel buton isteği varsa referans tarihini eski mantığımızla çözüyoruz
            if (machine.IsBackupRequested)
            {
                if (backupType == "Artımlı")
                {
                    var last = _context.Backups.Where(b => b.MachineId == machineId && b.Status == "Başarılı").OrderByDescending(b => b.BackupDate).FirstOrDefault();
                    referenceDate = last?.BackupDate;
                }
                else if (backupType == "Fark")
                {
                    var lastFull = _context.Backups.Where(b => b.MachineId == machineId && b.Status == "Başarılı" && b.BackupType == "Tam").OrderByDescending(b => b.BackupDate).FirstOrDefault();
                    referenceDate = lastFull?.BackupDate;
                }

                machine.IsBackupRequested = false;
                machine.RequestedBackupType = null;
            }

            _context.SaveChanges();

            return Ok(new
            {
                message = "Heartbeat alındı.",
                forceBackup = shouldBackup,
                backupType = backupType ?? "Artımlı",
                referenceDate = referenceDate
            });
        }

        [HttpPost("upload-backup")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = long.MaxValue)]
        public async Task<IActionResult> UploadBackup([FromForm] int machineId, [FromForm] string backupType, IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest();

            var machine = _context.Machines.Find(machineId);
            if (machine == null) return NotFound();

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "backups");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{machine.Name}_{backupType}_{DateTime.Now:yyyyMMdd_HHmmss}_{file.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            var backupRecord = new BackupRecord
            {
                MachineId = machine.Id,
                BackupDate = DateTime.Now,
                FileName = uniqueFileName,
                FileSize = Math.Round(file.Length / 1024.0 / 1024.0, 2),
                Status = "Başarılı",
                BackupType = backupType ?? "Tam" // Yeni alanımızı da kaydediyoruz
            };

            _context.Backups.Add(backupRecord);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Yedek başarıyla alındı." });
        }

        // PARÇALI (CHUNKED) DOSYA YÜKLEME METODU
        [HttpPost("upload-chunk")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = long.MaxValue)]
        public async Task<IActionResult> UploadChunk(
            [FromForm] int machineId,
            [FromForm] string backupType,
            [FromForm] string fileName,
            [FromForm] int chunkIndex,
            [FromForm] int totalChunks,
            IFormFile chunk)
        {
            var machine = _context.Machines.Find(machineId);
            if (machine == null) return NotFound("Makine bulunamadı.");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "backups");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            // Tüm parçaların aynı dosyaya yazılması için fileName'i kullanıyoruz
            var filePath = Path.Combine(uploadsFolder, fileName);

            // İlk parça geliyorsa dosyayı sıfırdan oluştur (Create), diğer parçalar geliyorsa üzerine ekle (Append)
            using (var fileStream = new FileStream(filePath, chunkIndex == 0 ? FileMode.Create : FileMode.Append))
            {
                await chunk.CopyToAsync(fileStream);
            }

            // Eğer gelen parça SON PARÇAYSA (Yükleme Bittiyse) veritabanı kaydını oluştur
            if (chunkIndex == totalChunks - 1)
            {
                var fileInfo = new FileInfo(filePath);
                var backupRecord = new BackupRecord
                {
                    MachineId = machine.Id,
                    BackupDate = DateTime.Now,
                    FileName = fileName,
                    // Birleştirilmiş dev dosyanın son boyutunu hesaplıyoruz
                    FileSize = Math.Round(fileInfo.Length / 1024.0 / 1024.0, 2),
                    Status = "Başarılı",
                    BackupType = backupType ?? "Tam"
                };

                _context.Backups.Add(backupRecord);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Tüm parçalar başarıyla birleştirildi ve yedek tamamlandı!" });

                // --- E-POSTA BİLDİRİMİ TETİKLEME ---
                var notifier = HttpContext.RequestServices.GetRequiredService<NotificationService>();

                string successMessage = $"<h3>✅ Yedekleme Başarıyla Tamamlandı!</h3>" +
                                        $"<b>Makine:</b> {machine.Name}<br>" +
                                        $"<b>Yedek Tipi:</b> {backupType}<br>" +
                                        $"<b>Dosya Boyutu:</b> {backupRecord.FileSize} MB<br>" +
                                        $"<b>Tarih:</b> {DateTime.Now:dd.MM.yyyy HH:mm:ss}";

                await notifier.SendEmailNotificationAsync($"✅ Başarılı Yedek: {machine.Name}", successMessage);

                return Ok(new { message = "Tüm parçalar birleştirildi ve yedek tamamlandı!" });
            }

            // Henüz bitmediyse Agent'a devam etmesini söyle
            return Ok(new { message = $"Parça {chunkIndex + 1}/{totalChunks} başarıyla alındı." });
        }


    }
}