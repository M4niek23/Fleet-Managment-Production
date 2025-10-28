using Fleet_Managment_Production.Models;
using Fleet_Managment_Production.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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


        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            return View(users);
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
    }
}