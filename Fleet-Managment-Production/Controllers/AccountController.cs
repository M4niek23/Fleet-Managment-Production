using Fleet_Managment_Production.Data;
using Fleet_Managment_Production.Models;
using Fleet_Managment_Production.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fleet_Managment_Production.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<Users> signInManager;
        private readonly UserManager<Users> userManager;
        private readonly RoleManager<IdentityRole> roleManager;

        public AccountController(SignInManager<Users> signInManager, UserManager<Users> userManager, RoleManager<IdentityRole> roleManager)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.roleManager = roleManager;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                var user = await userManager.FindByEmailAsync(model.Email);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty,"Nieprawidłowa próba logowania.");
                    return View(model);
                }

                var roles = await userManager.GetRolesAsync(user);

                if (roles.Count == 0)
                {
                    await signInManager.SignOutAsync();

                    ModelState.AddModelError(string.Empty, "Konto nie zostało jeszcze aktywowane. Skontaktuj się z administratorem.");
                    return View(model);
                }

                return RedirectToAction("Index", "Home");
            }
            if(result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Konto zostało zablokowane z powodu zbyt wielu nieudanych prób logowania. Spróbuj ponownie później.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Nieprawidłowy e-mail lub hasło");
            return View(model);
        }
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new Users
            {
                FullName = model.Name,
                UserName = model.Email,
                NormalizedUserName = model.Email.ToUpper(),
                Email = model.Email,
                NormalizedEmail = model.Email.ToUpper(),
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                return RedirectToAction("PendingApproval");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [AllowAnonymous]
        public IActionResult PendingApproval()
        {
            return View();
        }

        [HttpGet]
        public IActionResult VerifyEmail()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Nie znaleziono użytkownika o podanym adresie email.");
                return View(model);
            }

            var token = await userManager.GeneratePasswordResetTokenAsync(user);

            var encodedToken = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(token));

            return RedirectToAction("ResetPassword", "Account", new { token = encodedToken, email = user.Email });
        }

        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (token == null || email == null) return RedirectToAction("Login");

            var decodedToken = System.Text.Encoding.UTF8.GetString(Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(token));

            return View(new ResetPasswordViewModel { Token = decodedToken, Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await userManager.FindByEmailAsync(model.Email);
            if (user == null) return RedirectToAction("Login", "Account");

            var result = await userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);

            if (result.Succeeded)
            {
                ViewBag.Message = "Hasło zmienione pomyślnie. Za 5 sekund nastąpi przekierowanie...";
                return View("ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyAccount([FromServices] AppDbContext context)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Nie znaleziono użytkownika.");
            }
           
            var roles = await userManager.GetRolesAsync(user);
            var totalVehicles = await context.Vehicles.CountAsync(v => v.UserId == user.Id);
            var inUseVehicles = await context.Vehicles.CountAsync(v => v.UserId == user.Id && v.Status == VehicleStatus.InUse);
            var inMaintenanceVehicles = await context.Vehicles.CountAsync(v => v.UserId == user.Id && v.Status == VehicleStatus.InMaintenance);

            var totalCosts = await context.Costs
                .Where(c => c.Vehicle.UserId == user.Id)
                .SumAsync(c => c.Kwota);

            var totalDistance = await context.Trips
                .Where(t => t.Vehicle.UserId == user.Id && t.EndOdometer != null && t.EndOdometer > t.StartOdometer)
                .SumAsync(t => t.EndOdometer.Value - t.StartOdometer);

            var activeServices = await context.Services
                .Where(s => s.Vehicle.UserId == user.Id && s.ActualEndDate == null)
                .CountAsync();

            var model = new MyAccountViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber ?? "Brak",
                Roles = roles,
                TotalVehiclesCount = totalVehicles,
                VehiclesInUseCount = inUseVehicles,
                VehiclesInMaintenanceCount = inMaintenanceVehicles,
                TotalCosts = totalCosts,
                TotalDistanceDriven = totalDistance,
                ActiveServicesCount = activeServices
            };

            return View(model);
        }
        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Nie znaleziono użytkownika.");
            }
            var isPasswordCorrect = await userManager.CheckPasswordAsync(user, model.CurrentPassword);
          
            if (!isPasswordCorrect)
            {
            ModelState.AddModelError(nameof(model.CurrentPassword), "Obecne hasło jest nieprawidłowe.");
            return View(model);
            }
            var result = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await signInManager.RefreshSignInAsync(user);
                ModelState.AddModelError(nameof(model.NewPassword), "Hasło zostało zmienione");
                return View();
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

    }
}