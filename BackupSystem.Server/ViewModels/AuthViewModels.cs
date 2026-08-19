namespace BackupSystem.Server.ViewModels
{
    // 1. Kullanıcı Giriş Modeli
    public class LoginViewModel
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
    }

    // 2. Google Authenticator Kurulum Modeli (QR Kod için)
    public class Setup2FAViewModel
    {
        public string? AuthenticatorKey { get; set; }
        public string? QrCodeImageBase64 { get; set; }
        public string? Code { get; set; }
    }

    // 3. 2FA Doğrulama Modeli (Telefondaki 6 haneli şifre için)
    public class Verify2FAViewModel
    {
        public string? Code { get; set; }
    }
}