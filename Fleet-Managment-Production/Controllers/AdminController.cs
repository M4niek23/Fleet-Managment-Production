using Fleet_Managment_Production.Models;
using Fleet_Managment_Production.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fleet_Managment_Production.Controllers
{

    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<Users> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<Users> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }


        public async Task<IActionResult> Index(string searchString, string sortOrder, int? page)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["EmailSortParm"] = sortOrder == "Email" ? "email_desc" : "Email";
            ViewData["CurrentFilter"] = searchString;

            var usersQuery = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                var lowerSearch = searchString.ToLower();
                usersQuery = usersQuery.Where(u =>
                    (u.FullName != null && u.FullName.ToLower().Contains(lowerSearch)) ||
                    (u.UserName != null && u.UserName.ToLower().Contains(lowerSearch)) ||
                    (u.Email != null && u.Email.ToLower().Contains(lowerSearch))
                );

            }
            usersQuery = sortOrder switch
            {
                "name_desc" => usersQuery.OrderByDescending(u => u.FullName),
                "Email" => usersQuery.OrderBy(u => u.Email),
                "email_desc" => usersQuery.OrderByDescending(u => u.Email),
                _ => usersQuery.OrderBy(u => u.FullName),
            };

            int pageSize = 7;
            int pageNumber = page ?? 1;
            var totalItems = await usersQuery.CountAsync();

            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var usersList = await usersQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(usersList);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockUser(string id)
        {
           if (string.IsNullOrEmpty(id))
            {
                return NotFound("Nie podano ID użytkownika.");
            }
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound("Nie znaleziono użytkownika.");
            }
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);
            TempData["SuccessMessage"] = $"Konto użytkownika {user.Email} zostało pomyślnie odblokowane.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ManageRoles(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }



            var allRoles = await _roleManager.Roles.ToListAsync();


            var model = new ManagerUserRolesViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                Roles = new List<RoleSelectionViewModel>()
            };


            foreach (var role in allRoles)
            {
                model.Roles.Add(new RoleSelectionViewModel
                {
                    RoleName = role.Name,
                    IsSelected = await _userManager.IsInRoleAsync(user, role.Name)
                });
            }

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> ManageRoles(ManagerUserRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            var result = await _userManager.RemoveFromRolesAsync(user, userRoles);
            if (!result.Succeeded)
            {

                ModelState.AddModelError("", "Nie można usunąć ról użytkownika.");
                return View(model);
            }


            result = await _userManager.AddToRolesAsync(user,
                model.Roles.Where(r => r.IsSelected).Select(r => r.RoleName));

            if (!result.Succeeded)
            {

                ModelState.AddModelError("", "Nie można dodać ról do użytkownika.");
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBlock(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var targetUser = await _userManager.FindByIdAsync(id);
            if (targetUser == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            if (targetUser.Id == currentUserId)
            {
                TempData["ErrorMessage"] = "Nie możesz zablokować własnego konta.";
                return RedirectToAction(nameof(Index));
            }

            if (!targetUser.LockoutEnabled)
            {
                targetUser.LockoutEnabled = true;
                await _userManager.UpdateAsync(targetUser);
            }

            var isLocked = await _userManager.IsLockedOutAsync(targetUser);

            if (isLocked)
            {
                await _userManager.SetLockoutEndDateAsync(targetUser, null);
                TempData["SuccessMessage"] = $"Konto {targetUser.Email} zostało pomyślnie odblokowane.";
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(targetUser, DateTimeOffset.MaxValue);

                await _userManager.UpdateSecurityStampAsync(targetUser);

                TempData["SuccessMessage"] = $"Konto {targetUser.Email} zostało zablokowane.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}