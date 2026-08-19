using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BackupSystem.Agent
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = new HttpClient();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var serverUrl = _configuration["AgentSettings:ServerBaseUrl"];
            var machineId = int.Parse(_configuration["AgentSettings:MachineId"]);

            _logger.LogInformation($"Agent başlatıldı. Hedef Sunucu: {serverUrl} - Makine ID: {machineId}");

            while (!stoppingToken.IsCancellationRequested)
            {
                // 1. GÖREV: Heartbeat Gönderimi
                await SendHeartbeatAsync(serverUrl, machineId, stoppingToken);

                // 2. GÖREV: Yedekleme ve Upload İşlemi
                /*await TakeBackupAndUploadAsync(serverUrl, machineId, stoppingToken);
                */

                // Gerçek senaryoda bu süre saatlik/günlük olmalıdır. 
                // Test için şimdilik 30 saniyede bir (30000 ms) çalışacak şekilde ayarladık.
                await Task.Delay(5000, stoppingToken);
            }
        }

        private async Task SendHeartbeatAsync(string serverUrl, int machineId, CancellationToken stoppingToken)
        {
            try
            {
                var heartbeatUrl = $"{serverUrl}/api/agent/heartbeat/{machineId}";
                var response = await _httpClient.PostAsync(heartbeatUrl, null, stoppingToken);

                if (response.IsSuccessStatusCode)
                {
                    // Sunucudan gelen JSON cevabını okuyoruz
                    var responseString = await response.Content.ReadAsStringAsync(stoppingToken);
                    using var jsonDoc = JsonDocument.Parse(responseString);
                    var root = jsonDoc.RootElement;

                    bool forceBackup = false;
                    string? backupType = "Tam";
                    DateTime? referenceDate = null;
                    string? excludedExtensions = "";
                    string? excludedFolders = "";

                    if (root.TryGetProperty("forceBackup", out var forceProp)) 
                        forceBackup = forceProp.GetBoolean();

                    if (root.TryGetProperty("backupType", out var typeProp) && typeProp.ValueKind != System.Text.Json.JsonValueKind.Null)
                        backupType = typeProp.GetString();

                    if (root.TryGetProperty("referenceDate", out var dateProp) && dateProp.ValueKind != System.Text.Json.JsonValueKind.Null)
                        referenceDate = dateProp.GetDateTime();

                    if (root.TryGetProperty("excludedExtensions", out var extProp) && extProp.ValueKind != System.Text.Json.JsonValueKind.Null)
                        excludedExtensions = extProp.GetString();

                    if (root.TryGetProperty("excludedFolders", out var folderProp) && folderProp.ValueKind != System.Text.Json.JsonValueKind.Null)
                        excludedFolders = folderProp.GetString();

                    if (forceBackup)
                    {
                        _logger.LogWarning($"⚠️ SUNUCUDAN [{backupType}] YEDEKLEME EMRİ ALINDI!");
                        // Metoda excludedExtensions'ı da parametre olarak gönderiyoruz
                        await TakeBackupAndUploadAsync(serverUrl, machineId, backupType, referenceDate, excludedExtensions, excludedFolders, stoppingToken);
                    }
                }
                else
                {
                    _logger.LogWarning($"[{DateTime.Now:HH:mm:ss}] Sunucuya ulaşılamadı. Durum: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Heartbeat hatası: {ex.Message}");
            }
        }

        private async Task TakeBackupAndUploadAsync(string serverUrl, int machineId, string backupType, DateTime? referenceDate, string excludedExtensions, string excludedFolders, CancellationToken stoppingToken)
        {
            var targetDir = _configuration["AgentSettings:TargetDirectory"];
            if (!Directory.Exists(targetDir)) return;

            // Hariç tutulacak uzantıları temiz bir listeye çevir (Örn: ".tmp", ".log")
            var extList = string.IsNullOrEmpty(excludedExtensions)
                ? new List<string>()
                : excludedExtensions.Split(',').Select(e => e.Trim().ToLower()).ToList();

            var folderList = string.IsNullOrEmpty(excludedFolders) 
                ? new List<string>() 
                : excludedFolders.Split(',').Select(f => f.Trim().ToLower()).ToList();

            var allFiles = Directory.GetFiles(targetDir, "*.*", SearchOption.AllDirectories);

            // MİMARİ DOKUNUŞ: Hem Tarihe Göre Hem Uzantıya Göre Çift Filtreleme
            var filesToBackup = allFiles.Where(filePath =>
            {
                var fileInfo = new FileInfo(filePath);

                // KONTROL 1: Gizli veya Sistem Dosyası mı? (Thumbs.db, desktop.ini vb. atla)
                if (fileInfo.Attributes.HasFlag(FileAttributes.Hidden) || fileInfo.Attributes.HasFlag(FileAttributes.System))
                    return false;

                // KONTROL 2: Yasaklı Klasör İçinde mi? (Örn: \node_modules\ içindeyse atla)
                // Windows (\) ve Linux (/) yollarını desteklemesi için iki türlü bakıyoruz
                bool isInExcludedFolder = folderList.Any(folder => filePath.ToLower().Contains($"\\{folder}\\") || filePath.ToLower().Contains($"/{folder}/"));
                if (isInExcludedFolder)
                    return false;

                // KONTROL 3: Yasaklı Uzantı mı? (Örn: .tmp ise atla)
                if (extList.Contains(fileInfo.Extension.ToLower()))
                    return false;

                // KONTROL 4: Artımlı/Fark Yedek Tarih Kontrolü
                if (referenceDate != null && fileInfo.LastWriteTime <= referenceDate)
                    return false;

                // Tüm filtreleri başarıyla geçerse yedeğe dahil et!
                return true;
            }).ToList();

            if (!filesToBackup.Any())
            {
                _logger.LogInformation($"ℹ️ [{backupType}] Yedeklemesi: Dosya bulunamadı veya tüm dosyalar filtrelendi.");
                return;
            }
            // ... (Geri kalan Zipleme, Şifreleme ve UploadChunk kodları tamamen aynı kalacak)

            // ... Eski kodlar (Zip oluşturma kısmı)
            var tempZipPath = Path.Combine(Path.GetTempPath(), $"backup_{machineId}_{DateTime.Now:yyyyMMddHHmmss}.zip");
            var tempEncPath = tempZipPath + ".enc"; // Şifrelenmiş dosya için yeni uzantı

            try
            {
                _logger.LogInformation($"📦 [{backupType}] Yedeklemesi: {filesToBackup.Count} adet dosya sıkıştırılıyor...");

                using (var zip = ZipFile.Open(tempZipPath, ZipArchiveMode.Create))
                {
                    foreach (var file in filesToBackup)
                    {
                        var relativePath = Path.GetRelativePath(targetDir, file);
                        zip.CreateEntryFromFile(file, relativePath);
                    }
                }

                // MİMARİ DOKUNUŞ: Zip dosyasını şifrele!
                _logger.LogInformation("🔒 Dosya AES-256 ile şifreleniyor...");
                await EncryptFileAsync(tempZipPath, tempEncPath);

                // --- YENİ PARÇALI YÜKLEME BÖLÜMÜ BAŞLANGICI ---
                _logger.LogInformation("Sıkıştırma ve Şifreleme tamamlandı. Parçalı (Chunked) yükleme başlatılıyor...");

                var fileInfo = new FileInfo(tempEncPath);
                long fileSize = fileInfo.Length;
                int chunkSize = 10 * 1024 * 1024; // 10 MB (Her bir parçanın boyutu)
                int totalChunks = (int)Math.Ceiling((double)fileSize / chunkSize);

                // Sunucuda dosyaların birbirine karışmaması için sabit bir dosya adı oluşturuyoruz
                string uniqueFileName = $"backup_{machineId}_{backupType}_{DateTime.Now:yyyyMMddHHmm}.enc";

                using (var fileStream = new FileStream(tempEncPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    for (int i = 0; i < totalChunks; i++)
                    {
                        // 10 MB'lık geçici bir bellek (buffer) oluştur
                        byte[] buffer = new byte[chunkSize];
                        int bytesRead = await fileStream.ReadAsync(buffer, 0, chunkSize, stoppingToken);

                        if (bytesRead == 0) break; // Okunacak veri kalmadıysa çık

                        using var form = new MultipartFormDataContent();
                        form.Add(new StringContent(machineId.ToString()), "machineId");
                        form.Add(new StringContent(backupType), "backupType");
                        form.Add(new StringContent(uniqueFileName), "fileName");
                        form.Add(new StringContent(i.ToString()), "chunkIndex"); // Kaçıncı parça?
                        form.Add(new StringContent(totalChunks.ToString()), "totalChunks"); // Toplam parça sayısı

                        // Okunan byte'ları forma dosya parçası (chunk) olarak ekliyoruz
                        var chunkContent = new ByteArrayContent(buffer, 0, bytesRead);
                        form.Add(chunkContent, "chunk", uniqueFileName);

                        // Yeni yazdığımız "upload-chunk" API'sine atıyoruz
                        var uploadUrl = $"{serverUrl}/api/agent/upload-chunk";
                        var response = await _httpClient.PostAsync(uploadUrl, form, stoppingToken);

                        if (response.IsSuccessStatusCode)
                        {
                            // Gönderim yüzdesini konsola yazdır
                            int percentage = ((i + 1) * 100) / totalChunks;
                            _logger.LogInformation($"⏳ Yükleniyor... %{percentage} ({i + 1}/{totalChunks} Parça)");
                        }
                        else
                        {
                            _logger.LogError($"❌ Hata: Yükleme {i + 1}. parçada kesildi!");
                            break; // Hata alırsak döngüyü ve yüklemeyi durdur
                        }
                    }
                }

                _logger.LogInformation($"✅ [{backupType}] yedeği sunucuya BAŞARIYLA gönderildi!");
                // --- PARÇALI YÜKLEME BÖLÜMÜ BİTİŞİ ---
            }
            catch (Exception ex)
            {
                _logger.LogError($"Hata: {ex.Message}");
            }
            finally
            {
                if (File.Exists(tempZipPath)) File.Delete(tempZipPath);
                if (File.Exists(tempEncPath)) File.Delete(tempEncPath);
            }
        }

        // DOSYA ŞİFRELEME (AES-256) METODU
        private async Task EncryptFileAsync(string inputFile, string outputFile)
        {
            // Güvenlik için 32 karakterlik (256-bit) bir Anahtar (Key) ve 16 karakterlik bir Vektör (IV) belirliyoruz
            // Gerçek projelerde bu şifreler appsettings.json'dan gizlice çekilir
            byte[] key = System.Text.Encoding.UTF8.GetBytes("YedeklemeSistemiStajProjesi12345"); // 32 byte
            byte[] iv = System.Text.Encoding.UTF8.GetBytes("1234567890123456"); // 16 byte

            using var aes = System.Security.Cryptography.Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var inputFileStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read);
            using var outputFileStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write);

            // Verileri yazarken anında şifreleyen özel bir akış (CryptoStream) kullanıyoruz
            using var cryptoStream = new System.Security.Cryptography.CryptoStream(outputFileStream, aes.CreateEncryptor(), System.Security.Cryptography.CryptoStreamMode.Write);

            await inputFileStream.CopyToAsync(cryptoStream);
        }
    }
}