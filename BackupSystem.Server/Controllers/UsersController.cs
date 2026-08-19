using BackupSystem.Server.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackupSystem.Server.Controllers
{

    [Authorize]
    public class UsersController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // 1. KULLANICILARI LİSTELE
        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            var model = new List<UserListViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                model.Add(new UserListViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    Roles = roles
                });
            }
            return View(model);
        }

        // 2. YENİ KULLANICI EKLE EKRANI
        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Güvenlik: Eğer 'Kullanici' adında bir rol veritabanında yoksa, otomatik oluştur
                if (!await _roleManager.RoleExistsAsync("Kullanici"))
                    await _roleManager.CreateAsync(new IdentityRole("Kullanici"));

                var user = new IdentityUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Seçilen rolü (Admin veya Kullanici) kullanıcıya ata
                    await _userManager.AddToRoleAsync(user, model.Role);
                    TempData["Message"] = "Kullanıcı başarıyla oluşturuldu.";
                    return RedirectToAction("Index");
                }

                // Şifre kurallarına uyulmazsa hataları ekrana bas (Örn: En az 1 büyük harf vs.)
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
            }
            return View(model);
        }

        // 3. KULLANICI SİLME
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                // Sistemi kilitlememek için kendi kendini silmeyi engelliyoruz
                if (user.Email == User.Identity.Name)
                {
                    TempData["Error"] = "Kendi hesabınızı silemezsiniz!";
                    return RedirectToAction("Index");
                }

                await _userManager.DeleteAsync(user);
                TempData["Message"] = "Kullanıcı başarıyla silindi.";
            }
            return RedirectToAction("Index");
        }
    }
}