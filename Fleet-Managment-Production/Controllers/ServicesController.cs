using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Fleet_Managment_Production.Data;
using Fleet_Managment_Production.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Fleet_Managment_Production.Controllers
{
    [Authorize]
    public class ServicesController(AppDbContext context, UserManager<Users> userManager) : Controller
    {

        // GET: Services
        public async Task<IActionResult> Index(string searchString,int? page)
        {
            ViewData["CurrentFilter"] = searchString;

            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();
            var isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");


          
            var servicesQuery = context.Services
                .Include(s => s.Vehicle)
                .ThenInclude(v => v!.Driver)
                .OrderByDescending(s => s.EntryDate)
                .AsQueryable();

            if (!isAdminOrManager)
            {
                servicesQuery = servicesQuery.Where(s => s.Vehicle != null && s.Vehicle.Driver != null && s.Vehicle.Driver.UserId == currentUser.Id);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                servicesQuery = servicesQuery.Where(s =>
                    (s.Description != null && s.Description.Contains(searchString)) ||
                    (s.Vehicle != null && s.Vehicle.Make != null && s.Vehicle.Make.Contains(searchString)) ||
                    (s.Vehicle != null && s.Vehicle.Model != null && s.Vehicle.Model.Contains(searchString)) ||
                    (s.Vehicle != null && s.Vehicle.LicensePlate != null && s.Vehicle.LicensePlate.Contains(searchString))
                );
            }
            int pageSize = 8;
            int pageNumber = page ?? 1;
            var totalItems = await servicesQuery.CountAsync();

            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var servicesList = await servicesQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(servicesList);
        }

        // GET: Services/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var service = await context.Services
                .Include(s => s.Vehicle)
                .ThenInclude(v => v!.Driver)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (service == null) return NotFound();

            // Bezpieczeństwo
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();
            if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
            {
                if (service.Vehicle?.Driver?.UserId != currentUser.Id) return Forbid();
            }

            return View(service);
        }

        // GET: Services/Create
        public async Task<IActionResult> Create()
        {
            await PopulateVehiclesDropdownAsync();
            return View();
        }

        // POST: Services/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,VehicleId,Description,Cost,EntryDate,PlannedEndDate,ActualEndDate")] Service service)
        {
            var vehicle = await context.Vehicles.FindAsync(service.VehicleId);
            if (vehicle == null) return NotFound();

            if (vehicle.Status != VehicleStatus.InMaintenance)
            {
                ModelState.AddModelError("", "Nie można oddać samochodu do serwisu, ponieważ ma inny status niż 'W serwisie'. Zmień status pojazdu przed dodaniem naprawy.");
                await PopulateVehiclesDropdownAsync(service.VehicleId);
                return View(service);
            }
            if (service.PlannedEndDate.HasValue && service.PlannedEndDate.Value < service.EntryDate)
            {
                ModelState.AddModelError("PlannedEndDate", "Planowana data zakończenia nie może być wcześniejsza niż data rozpoczęcia.");
            }
            if (service.ActualEndDate.HasValue && service.ActualEndDate.Value < service.EntryDate)
            {
                ModelState.AddModelError("ActualEndDate", "Rzeczywista data zakończenia nie może być wcześniejsza niż data rozpoczęcia.");
            }
            if (ModelState.IsValid)
            {
                context.Add(service);
                await context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            await PopulateVehiclesDropdownAsync(service.VehicleId);
            return View(service);
        }

        // GET: Services/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var service = await context.Services
                .Include(s => s.Vehicle)
                .ThenInclude(v => v!.Driver)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (service == null) return NotFound();

            // Bezpieczeństwo
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();
            if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
            {
                if (service.Vehicle?.Driver?.UserId != currentUser.Id) return Forbid();
            }

            await PopulateVehiclesDropdownAsync(service.VehicleId);
            return View(service);
        }

        // POST: Services/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,VehicleId,Description,Cost,EntryDate,PlannedEndDate,ActualEndDate")] Service service)
        {
            if (id != service.Id) return NotFound();

            var originalService = await context.Services.AsNoTracking().Include(s => s.Vehicle).ThenInclude(v => v!.Driver).FirstOrDefaultAsync(s => s.Id == id);
            if (originalService == null) return NotFound();

            // Bezpieczeństwo
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();
            if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
            {
                if (originalService.Vehicle?.Driver?.UserId != currentUser.Id) return Forbid();
            }
            if (service.PlannedEndDate.HasValue && service.PlannedEndDate.Value < service.EntryDate)
            {
                ModelState.AddModelError("PlannedEndDate", "Planowana data zakończenia nie może być wcześniejsza niż data rozpoczęcia.");
            }
            if (service.ActualEndDate.HasValue && service.ActualEndDate.Value < service.EntryDate)
            {
                ModelState.AddModelError("ActualEndDate", "Rzeczywista data zakończenia nie może być wcześniejsza niż data rozpoczęcia.");
            }
            if (ModelState.IsValid)
            {
                try
                {
                    if (service.ActualEndDate.HasValue)
                    {
                        var vehicle = await context.Vehicles.FindAsync(service.VehicleId);
                        if (vehicle != null && vehicle.Status == VehicleStatus.InMaintenance)
                        {
                            vehicle.Status = VehicleStatus.Available;
                            context.Update(vehicle);
                        }
                    }

                    context.Update(service);
                    await context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServiceExists(service.Id)) return NotFound();
                    else throw;
                }
            }

            await PopulateVehiclesDropdownAsync(service.VehicleId);
            return View(service);
        }

        // GET: Services/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var service = await context.Services
                .Include(s => s.Vehicle)
                .ThenInclude(v => v!.Driver)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (service == null) return NotFound();

            // Bezpieczeństwo
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();
            if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
            {
                if (service.Vehicle?.Driver?.UserId != currentUser.Id) return Forbid();
            }

            return View(service);
        }

        // POST: Services/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var service = await context.Services.FindAsync(id);
            if (service != null)
            {
                context.Services.Remove(service);
                await context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ServiceExists(int id)
        {
            return context.Services.Any(e => e.Id == id);
        }

        private async Task PopulateVehiclesDropdownAsync(object? selectedVehicle = null)
        {
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null) return;
            var isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");

            var vehiclesQuery = context.Vehicles
                .Where(v => v.Status == VehicleStatus.InMaintenance)
                .AsQueryable();

            if (!isAdminOrManager)
            {
                vehiclesQuery = vehiclesQuery.Where(v => v.Driver != null && v.Driver.UserId == currentUser.Id);
            }

            var vehicles = await vehiclesQuery
                  .Select(v => new
                  {
                      v.VehicleId,
                      DisplayName = v.Make + " " + v.Model + " (" + v.LicensePlate + ")"
                  })
                  .OrderBy(v => v.DisplayName)
                  .ToListAsync();

            if (vehicles.Count == 0)
            {
                var emptyList = new List<object>
                {
                    new { VehicleId = "", DisplayName = "Brak pojazdów o statusie 'W serwisie'" }
                };
                ViewBag.VehicleId = new SelectList(emptyList, "VehicleId", "DisplayName");
            }
            else
            {
                ViewBag.VehicleId = new SelectList(vehicles, "VehicleId", "DisplayName", selectedVehicle);
            }
        }
    }
}