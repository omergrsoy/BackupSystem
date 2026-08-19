using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace BackupSystem.Server.Services
{
    public class FtpUploadService
    {
        private readonly IConfiguration _configuration;

        public FtpUploadService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task UploadFileAsync(string localFilePath, string fileName)
        {
            // Ayarlardan FTP'nin açık olup olmadığını kontrol et
            var isEnabled = _configuration.GetValue<bool>("FtpSettings:IsEnabled");
            if (!isEnabled) return;

            var serverUrl = _configuration["FtpSettings:ServerUrl"];
            var username = _configuration["FtpSettings:Username"];
            var password = _configuration["FtpSettings:Password"];

            try
            {
                // Eğer sahte ayarlar duruyorsa simüle et ve çık (Proje çökmesin)
                if (serverUrl == "ftp://sizin-ftp-adresiniz.com/backups/")
                {
                    Console.WriteLine($"☁️ [FTP SİMÜLASYONU]: {fileName} dosyası uzak sunucuya aktarılmış gibi varsayıldı.");
                    return;
                }

                Console.WriteLine($"☁️ FTP Yüklemesi Başlıyor: {fileName}");

                // FTP İsteğini Hazırla
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(serverUrl + fileName);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(username, password);
                request.UsePassive = true;
                request.UseBinary = true;
                request.KeepAlive = false;

                // Dosyayı sunucunun diskinden oku ve stream (akış) halinde FTP'ye yaz
                using (FileStream fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read))
                {
                    using (Stream requestStream = await request.GetRequestStreamAsync())
                    {
                        await fileStream.CopyToAsync(requestStream);
                    }
                }

                Console.WriteLine($"✅ Dosya FTP sunucusuna başarıyla yüklendi: {fileName}");

                // Opsiyonel: FTP'ye atıldıktan sonra sunucudaki yerel dosyayı silmek isterseniz:
                // if (File.Exists(localFilePath)) File.Delete(localFilePath); 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ FTP Yükleme Hatası: {ex.Message}");
            }
        }
    }
}