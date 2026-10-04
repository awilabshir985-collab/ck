using BeeciyeMarket.Data;
using BeeciyeMarket.Models;
using BeeciyeMarket.Models.ViewModels;
using BeeciyeMarket.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BeeciyeMarket.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _environment;
        private readonly INotificationService _notifications;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IWebHostEnvironment environment, INotificationService notifications)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _environment = environment;
            _notifications = notifications;
        }

        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    Address = model.Address,
                    City = model.City,
                    CreatedAt = DateTime.Now
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, Roles.User);

                    await _notifications.NotifyAsync(user.Id,
                        "Welcome to Beeciye!",
                        "Your account is ready. Browse second-hand items or list your own to start selling.",
                        "/Product/Index",
                        NotificationType.Account);
                    await _notifications.NotifyAdminsAsync(
                        "New user registered",
                        $"{user.FullName} ({user.Email}) just created an account.",
                        "/Admin/Users",
                        NotificationType.Account);

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToLocal(returnUrl);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToLocal(returnUrl);
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            ViewData["ReturnUrl"] = model.ReturnUrl;

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    return RedirectToLocal(model.ReturnUrl);
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ApplicationUser model, IFormFile? profileImage)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;
            user.City = model.City;

            if (profileImage != null && profileImage.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(profileImage.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension) || !profileImage.ContentType.StartsWith("image/"))
                {
                    ModelState.AddModelError(string.Empty, "Please upload a valid image file.");
                    return View(user);
                }

                user.ProfileImagePath = await SaveProfileImageAsync(profileImage);
            }

            await _userManager.UpdateAsync(user);
            ViewBag.Saved = true;
            return View(user);
        }

        private async Task<string> SaveProfileImageAsync(IFormFile file)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "profiles");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/images/profiles/{fileName}";
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
