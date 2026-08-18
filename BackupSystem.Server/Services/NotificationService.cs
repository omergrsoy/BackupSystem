using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace BackupSystem.Server.Services
{
    public class NotificationService
    {

        private const string SmtpHost = "smtp.gmail.com";
        private const int SmtpPort = 587;
        private const string SenderEmail = "omergrsoy52@gmail.com";
        private const string SenderPassword = "dfny djhp dmtq pjfn";
        private const string ReceiverEmail = "omergrsy0652@hotmail.com"; // Kendi e-postanızı yazın

        public async Task SendEmailNotificationAsync(string subject, string htmlMessage)
        {
            try
            {
                // Ayarlar varsayılan bırakıldıysa konsolda simüle et (Hata vermesin)
                if (SenderEmail == "omergrsoy52@gmail.com")
                {
                    Console.WriteLine($"📧 [E-POSTA SİMÜLASYONU] | Konu: {subject} | Mesaj: {htmlMessage.Replace("<br>", " ")}");
                    return;
                }

                using var client = new SmtpClient(SmtpHost, SmtpPort)
                {
                    Credentials = new NetworkCredential(SenderEmail, SenderPassword),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(SenderEmail, "Yedekleme Sistemi Otomasyonu"),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true // HTML formatında gönderiyoruz
                };

                mailMessage.To.Add(ReceiverEmail);

                await client.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"E-Posta gönderilirken hata oluştu: {ex.Message}");
            }
        }
    }
}