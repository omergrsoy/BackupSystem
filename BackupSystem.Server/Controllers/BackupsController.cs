using BackupSystem.Server.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace BackupSystem.Server.Controllers
{
    [Authorize]
    public class BackupsController : Controller
    {
        private readonly AppDbContext _context;

        public BackupsController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Geçmiş yedekleri, makine bilgileriyle birlikte (Include) 
            // tarihe göre azalan (en yeni en üstte) sırayla çekiyoruz.
            var backups = _context.Backups
                                  .Include(b => b.Machine)
                                  .OrderByDescending(b => b.BackupDate)
                                  .ToList();

            return View(backups);
        }
        // YEDEK DOSYASI İNDİRME METODU
        // YEDEK DOSYASI İNDİRME VE ŞİFRE ÇÖZME METODU
        public IActionResult Download(int id)
        {
            var backup = _context.Backups.Find(id);
            if (backup == null) return NotFound("Yedek kaydı bulunamadı.");

            var filePath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", "backups", backup.FileName);
            if (!System.IO.File.Exists(filePath)) return NotFound("Fiziksel dosya sunucuda bulunamadı.");

            // Agent tarafında kullandığımız şifrenin ve vektörün BİREBİR AYNISI
            byte[] key = System.Text.Encoding.UTF8.GetBytes("YedeklemeSistemiStajProjesi12345");
            byte[] iv = System.Text.Encoding.UTF8.GetBytes("1234567890123456");

            var aes = System.Security.Cryptography.Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            // Dosyayı fiziksel diskten okumak için açıyoruz
            var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);

            // Okunan şifreli veriyi anında (stream üzerinden) çözen mekanizma
            var cryptoStream = new System.Security.Cryptography.CryptoStream(fileStream, aes.CreateDecryptor(), System.Security.Cryptography.CryptoStreamMode.Read);

            // Veritabanında ".enc" olarak kayıtlı ismi temizleyip kullanıcıya ".zip" olarak indireceğiz
            string downloadName = backup.FileName.EndsWith(".enc")
                ? backup.FileName.Substring(0, backup.FileName.Length - 4)
                : backup.FileName;

            // cryptoStream'i File() metoduna veriyoruz. ASP.NET Core dosya inerken şifreyi çözecektir.
            return File(cryptoStream, "application/zip", downloadName);
        }
    }
}