using BackupSystem.Server.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BackupSystem.Server.Services
{
    public class BackupCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BackupCleanupService> _logger;

        public BackupCleanupService(IServiceProvider serviceProvider, ILogger<BackupCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("🧹 Otomatik yedek temizleme servisi çalışıyor...");

                // BackgroundService içinde veritabanı (Scoped) kullanmak için Scope oluşturmalıyız
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var limitDate = DateTime.Now.AddDays(-7); // 7 günden eski yedekler
                    var notifier = scope.ServiceProvider.GetRequiredService<NotificationService>();

                    var oldBackups = context.Backups.Where(b => b.BackupDate < limitDate).ToList();

                    var offlineLimit = DateTime.Now.AddMinutes(-5);
                    var offlineMachines = context.Machines
                        .Where(m => m.IsActive && m.LastHeartbeat < offlineLimit)
                        .ToList();

                    foreach (var machine in offlineMachines)
                    {
                        // HTML formatında (<br> ile alt satıra geçerek) e-posta içeriği hazırlıyoruz
                        string alertMessage = $"<h3>⚠️ ALARM: Çevrimdışı Makine!</h3>" +
                                              $"<b>Makine Adı:</b> {machine.Name}<br>" +
                                              $"<b>IP Adresi:</b> {machine.IpAddress}<br>" +
                                              $"<b>Son Erişim:</b> {machine.LastHeartbeat:dd.MM.yyyy HH:mm:ss}<br><br>" +
                                              $"Lütfen acilen makinenin ağ bağlantısını ve Agent servisini kontrol edin.";

                        // Yeni e-posta servisini çağırıyoruz (Konu ve Mesaj olarak)
                        await notifier.SendEmailNotificationAsync($"⚠️ Sistem Uyarısı: {machine.Name} Çevrimdışı!", alertMessage);
                    }
                    foreach (var backup in oldBackups)
                    {
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "backups", backup.FileName);
                        if (File.Exists(filePath)) File.Delete(filePath);
                        context.Backups.Remove(backup);
                    }

                    if (oldBackups.Any())
                    {
                        await context.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation($"✅ {oldBackups.Count} adet eski yedek sistemden kalıcı olarak temizlendi.");
                    }
                }

                // Servisi her 24 saatte bir çalışacak şekilde uyutuyoruz
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }   
        }
    }
}