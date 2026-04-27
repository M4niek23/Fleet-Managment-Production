using Fleet_Managment_Production.Models;
using Fleet_Managment_Production.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fleet_Managment_Production.Controllers
{

    [Authorize(Roles = "Admin")]
    public class AdminController(UserManager<Users> userManager, RoleManager<IdentityRole> roleManager) : Controller
    {
        public async Task<IActionResult> Index(string searchString, string sortOrder, int? page)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["EmailSortParm"] = sortOrder == "Email" ? "email_desc" : "Email";
            ViewData["CurrentFilter"] = searchString;

            var usersQuery = userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                usersQuery = usersQuery.Where(u =>
                    (u.FullName != null && u.FullName.Contains(searchString)) ||
                    (u.UserName != null && u.UserName.Contains(searchString)) ||
                    (u.Email != null && u.Email.Contains(searchString))
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
            var user = await userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound("Nie znaleziono użytkownika.");
            }
            await userManager.SetLockoutEndDateAsync(user, null);
            await userManager.ResetAccessFailedCountAsync(user);
            TempData["SuccessMessage"] = $"Konto użytkownika {user.Email} zostało pomyślnie odblokowane.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ManageRoles(string userId)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }



            var allRoles = await roleManager.Roles.ToListAsync();


            var model = new ManagerUserRolesViewModel
            {
                UserId = user.Id,
                UserName = user.UserName ?? "Brak nazwy",
                Roles = []
            };


            foreach (var role in allRoles)
            {
              if(role.Name != null)
                {
                    model.Roles.Add(new RoleSelectionViewModel
                    {
                        RoleName = role.Name,
                        IsSelected = await userManager.IsInRoleAsync(user, role.Name)
                    });
                }
            }

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> ManageRoles(ManagerUserRolesViewModel model)
        {
            var user = await userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await userManager.GetRolesAsync(user);

            var result = await userManager.RemoveFromRolesAsync(user, userRoles);
            if (!result.Succeeded)
            {

                ModelState.AddModelError("", "Nie można usunąć ról użytkownika.");
                return View(model);
            }


            result = await userManager.AddToRolesAsync(user,
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

            var targetUser = await userManager.FindByIdAsync(id);
            if (targetUser == null) return NotFound();

            var currentUserId = userManager.GetUserId(User);
            if (targetUser.Id == currentUserId)
            {
                TempData["ErrorMessage"] = "Nie możesz zablokować własnego konta.";
                return RedirectToAction(nameof(Index));
            }

            if (!targetUser.LockoutEnabled)
            {
                targetUser.LockoutEnabled = true;
                await userManager.UpdateAsync(targetUser);
            }

            var isLocked = await userManager.IsLockedOutAsync(targetUser);

            if (isLocked)
            {
                await userManager.SetLockoutEndDateAsync(targetUser, null);
                TempData["SuccessMessage"] = $"Konto {targetUser.Email} zostało pomyślnie odblokowane.";
            }
            else
            {
                await userManager.SetLockoutEndDateAsync(targetUser, DateTimeOffset.MaxValue);

                await userManager.UpdateSecurityStampAsync(targetUser);

                TempData["SuccessMessage"] = $"Konto {targetUser.Email} zostało zablokowane.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}