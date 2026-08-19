using BackupSystem.Server.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
using System;
using System.Threading.Tasks;

namespace BackupSystem.Server.Controllers
{
    public class AuthController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public AuthController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        // --- 1. GİRİŞ EKRANI ---
        [AllowAnonymous]
        public IActionResult Login() => View();

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                // Şifreyi doğrula
                var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);

                if (result.Succeeded)
                    return RedirectToAction("Index", "Home");

                // Eğer kullanıcıda 2FA açıksa, kod doğrulama ekranına yönlendir
                if (result.RequiresTwoFactor)
                    return RedirectToAction("Verify2FA");
            }

            ModelState.AddModelError("", "E-posta veya şifre hatalı.");
            return View(model);
        }

        // --- 2. GOOGLE AUTHENTICATOR KURULUM EKRANI (QR KOD) ---
        [Authorize] // Sadece giriş yapmış biri 2FA kurabilir
        public async Task<IActionResult> Setup2FA()
        {
            var user = await _userManager.GetUserAsync(User);
            await _userManager.ResetAuthenticatorKeyAsync(user);
            var key = await _userManager.GetAuthenticatorKeyAsync(user);

            // QR Kodun tarandığında telefonda görünecek metni (Yedekleme Sistemi)
            string qrUri = $"otpauth://totp/YedeklemeSistemi:{user.Email}?secret={key}&issuer=YedeklemeSistemi&digits=6";

            // QRCoder kütüphanesi ile URI'yi karekoda (Resme) çeviriyoruz
            using QRCodeGenerator qrGenerator = new QRCodeGenerator();
            using QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrUri, QRCodeGenerator.ECCLevel.Q);
            using PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeImage = qrCode.GetGraphic(10);

            var model = new Setup2FAViewModel
            {
                AuthenticatorKey = key,
                QrCodeImageBase64 = Convert.ToBase64String(qrCodeImage)
            };

            return View(model);
        }

        // Kullanıcı QR'ı okutup kodu girdiğinde burası çalışır
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Setup2FA(Setup2FAViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            // Telefondan girilen kod doğru mu diye kontrol et
            var isValid = await _userManager.VerifyTwoFactorTokenAsync(user, _userManager.Options.Tokens.AuthenticatorTokenProvider, model.Code);

            if (isValid)
            {
                // Kod doğruysa hesabı 2FA korumasına al!
                await _userManager.SetTwoFactorEnabledAsync(user, true);
                TempData["Message"] = "Google Authenticator başarıyla kuruldu!";
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Girdiğiniz 6 haneli kod hatalı.");
            return View(model);
        }

        // --- 3. 2FA DOĞRULAMA EKRANI (GİRİŞ YAPARKEN) ---
        [AllowAnonymous]
        public IActionResult Verify2FA() => View();

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Verify2FA(Verify2FAViewModel model)
        {
            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(model.Code, false, false);
            if (result.Succeeded) return RedirectToAction("Index", "Home");

            ModelState.AddModelError("", "Girdiğiniz kod yanlış veya süresi dolmuş.");
            return View(model);
        }

        // --- ÇIKIŞ YAP ---
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }
    }
}