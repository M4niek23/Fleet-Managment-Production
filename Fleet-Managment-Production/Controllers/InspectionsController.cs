using Fleet_Managment_Production.Data;
using Fleet_Managment_Production.Models;
using Fleet_Managment_Production.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Fleet_Managment_Production.Controllers
{
    public class InspectionsController(AppDbContext context, UserManager<Users> userManager) : Controller
    {
        public async Task<IActionResult> Index(string searchString, int? vehicleId, int? page, string activeTab = "active")
        {
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");

            if (!context.Vehicles.Any()) return View("NoVehicles");

            ViewData["CurrentFilter"] = searchString;
            ViewBag.CurrentVehicleId = vehicleId;
            ViewBag.ActiveTab = activeTab;

            int pageSize = 7;
            int pageNumber = page ?? 1;

            var query = context.Inspections.Include(i => i.Vehicle).ThenInclude(v => v!.Driver).AsQueryable();

            if (!isAdminOrManager)
            {
                query = query.Where(i => i.Vehicle != null && (
                    i.Vehicle.UserId == currentUser.Id ||
                    (i.Vehicle.Driver != null && i.Vehicle.Driver.UserId == currentUser.Id)
                ));
            }

            if (vehicleId.HasValue)
            {
                query = query.Where(i => i.VehicleId == vehicleId.Value);
                var selectedVehicle = await context.Vehicles.FindAsync(vehicleId.Value);
                ViewBag.VehicleRegistration = selectedVehicle?.LicensePlate ?? "Wybranego pojazdu";
            }
            else
            {
                ViewBag.VehicleRegistration = "Wszystkie pojazdy";
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(i =>
                    i.Vehicle != null && (
                        (i.Vehicle.LicensePlate != null && i.Vehicle.LicensePlate.Contains(searchString)) ||
                        (i.Vehicle.Make != null && i.Vehicle.Make.Contains(searchString))
                    )
                );
            }

            var activeQ = query.Where(i => i.IsResultPositive != false && (i.NextInspectionDate == null || i.NextInspectionDate >= DateTime.Today));
            var negativeQ = query.Where(i => i.IsResultPositive == false);
            var historyQ = query.Where(i => i.IsResultPositive != false && i.NextInspectionDate < DateTime.Today);

            ViewBag.ActiveTotal = await activeQ.CountAsync();
            ViewBag.NegativeTotal = await negativeQ.CountAsync();
            ViewBag.HistoryTotal = await historyQ.CountAsync();

            int currentCount = activeTab switch
            {
                "negative" => ViewBag.NegativeTotal,
                "history" => ViewBag.HistoryTotal,
                _ => ViewBag.ActiveTotal
            };

            ViewBag.TotalPages = (int)Math.Ceiling(currentCount / (double)pageSize);
            ViewBag.CurrentPage = pageNumber;

            var viewModel = new InspectionsViewModel
            {
                ActiveInspections = await (activeTab == "active"
                    ? activeQ.OrderByDescending(i => i.InspectionDate).Skip((pageNumber - 1) * pageSize).Take(pageSize)
                    : activeQ.OrderByDescending(i => i.InspectionDate).Take(pageSize)).ToListAsync(),

                NegativeInspections = await (activeTab == "negative"
                    ? negativeQ.OrderByDescending(i => i.InspectionDate).Skip((pageNumber - 1) * pageSize).Take(pageSize)
                    : negativeQ.OrderByDescending(i => i.InspectionDate).Take(pageSize)).ToListAsync(),

                HistoricalInspections = await (activeTab == "history"
                    ? historyQ.OrderByDescending(i => i.InspectionDate).Skip((pageNumber - 1) * pageSize).Take(pageSize)
                    : historyQ.OrderByDescending(i => i.InspectionDate).Take(pageSize)).ToListAsync()
            };

            var vehiclesQuery = context.Vehicles.AsQueryable();
            if (!isAdminOrManager)
            {
                vehiclesQuery = vehiclesQuery.Where(v => v.UserId == currentUser.Id || (v.Driver != null && v.Driver.UserId == currentUser.Id));
            }
            var vehicleList = await vehiclesQuery.Select(v => new { v.VehicleId, Display = v.LicensePlate }).ToListAsync();
            ViewBag.VehicleList = new SelectList(vehicleList, "VehicleId", "Display", vehicleId);

            return View(viewModel);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();
            var isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");

            var inspection = await context.Inspections
                .Include(i => i.Vehicle)
                .ThenInclude(v => v!.Driver)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (inspection == null) return NotFound();

            if (!isAdminOrManager && (inspection.Vehicle?.UserId != currentUser.Id && inspection.Vehicle?.Driver?.UserId != currentUser.Id))
            {
                return Forbid();
            }

            return View(inspection);
        }

        public async Task<IActionResult> Create(int? vehicleId)
        {
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();
            var isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");

            var vehiclesQuery = context.Vehicles
                .Where(v => v.Status != VehicleStatus.Sold)
                .AsQueryable();

            if (!isAdminOrManager)
            {
                vehiclesQuery = vehiclesQuery.Where(v => v.UserId == currentUser.Id || (v.Driver != null && v.Driver.UserId == currentUser.Id));
            }

            var vehicleList = await vehiclesQuery
                .Select(v => new { v.VehicleId, DisplayText = v.LicensePlate ?? (v.Make + " " + v.Model) })
                .ToListAsync();

            ViewBag.VehicleList = new SelectList(vehicleList, "VehicleId", "DisplayText", vehicleId);

            return View(new Inspection { InspectionDate = DateTime.Today, IsActive = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,InspectionDate,Description,Mileage,Cost,VehicleId,IsResultPositive,IsActive")] Inspection inspection)
        {
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();
            var isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");

            var vehicle = await context.Vehicles.Include(v => v.Driver).FirstOrDefaultAsync(v => v.VehicleId == inspection.VehicleId);
            if (vehicle == null) return NotFound();

            if (!isAdminOrManager && vehicle.UserId != currentUser.Id && vehicle.Driver?.UserId != currentUser.Id)
            {
                return Forbid();
            }

            if (inspection.IsActive == true && inspection.InspectionDate.Date >= DateTime.Today)
            {
                var existingUpcoming = await context.Inspections
                    .FirstOrDefaultAsync(i => i.VehicleId == inspection.VehicleId &&
                                              i.InspectionDate.Date >= DateTime.Today &&
                                              i.IsActive == true);

                if (existingUpcoming != null)
                {
                    ModelState.AddModelError("VehicleId", "Ten pojazd ma już zaplanowany AKTYWNY przegląd.");
                }
            }

            if (inspection.IsResultPositive == true)
            {
                inspection.NextInspectionDate = inspection.InspectionDate.AddYears(1);
            }
            else if (inspection.IsResultPositive == false)
            {
                inspection.NextInspectionDate = inspection.InspectionDate.AddDays(14);
            }
            else
            {
                inspection.NextInspectionDate = null;
            }

            if (inspection.Mileage.HasValue)
            {
                if (inspection.Mileage.Value < vehicle.CurrentKm)
                {
                    ModelState.AddModelError("Mileage", $"Przebieg nie może być mniejszy niż aktualny stan licznika pojazdu ({vehicle.CurrentKm} km).");
                }
            }

            if (ModelState.IsValid)
            {
                if (inspection.Mileage.HasValue && inspection.Mileage.Value > vehicle.CurrentKm)
                {
                    vehicle.CurrentKm = inspection.Mileage.Value;
                    context.Update(vehicle);
                }

                context.Add(inspection);
                await context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var vehiclesQuery = context.Vehicles.Where(v => v.Status != VehicleStatus.Sold).AsQueryable();
            if (!isAdminOrManager)
            {
                vehiclesQuery = vehiclesQuery.Where(v => v.UserId == currentUser.Id || (v.Driver != null && v.Driver.UserId == currentUser.Id));
            }

            var vehicleList = await vehiclesQuery
                .Select(v => new { v.VehicleId, DisplayText = v.LicensePlate ?? (v.Make + " " + v.Model) })
                .ToListAsync();

            ViewBag.VehicleList = new SelectList(vehicleList, "VehicleId", "DisplayText", inspection.VehicleId);

            return View(inspection);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();
            var isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");

            var inspection = await context.Inspections
                .Include(i => i.Vehicle)
                .ThenInclude(v => v!.Driver)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inspection == null) return NotFound();

            if (!isAdminOrManager && (inspection.Vehicle?.UserId != currentUser.Id && inspection.Vehicle?.Driver?.UserId != currentUser.Id))
            {
                return Forbid();
            }

            var vehiclesQuery = context.Vehicles.AsQueryable();
            if (!isAdminOrManager)
            {
                vehiclesQuery = vehiclesQuery.Where(v => v.UserId == currentUser.Id || (v.Driver != null && v.Driver.UserId == currentUser.Id));
            }

            var vehicleList = await vehiclesQuery
                .Select(v => new { v.VehicleId, DisplayText = v.LicensePlate ?? (v.Make + " " + v.Model) })
                .ToListAsync();

            ViewBag.VehicleList = new SelectList(vehicleList, "VehicleId", "DisplayText", inspection.VehicleId);

            return View(inspection);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,InspectionDate,Description,Mileage,Cost,VehicleId,IsResultPositive,IsActive")] Inspection inspection)
        {
            if (id != inspection.Id) return NotFound();

            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();
            var isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");

            var vehicle = await context.Vehicles.Include(v => v.Driver).FirstOrDefaultAsync(v => v.VehicleId == inspection.VehicleId);
            if (vehicle == null) return NotFound();

            if (!isAdminOrManager && vehicle.UserId != currentUser.Id && vehicle.Driver?.UserId != currentUser.Id)
            {
                return Forbid();
            }

            if (inspection.IsActive == true && inspection.InspectionDate.Date >= DateTime.Today)
            {
                var existingUpcoming = await context.Inspections
                    .FirstOrDefaultAsync(i => i.VehicleId == inspection.VehicleId &&
                                              i.InspectionDate.Date >= DateTime.Today &&
                                              i.IsActive == true &&
                                              i.Id != inspection.Id);

                if (existingUpcoming != null)
                {
                    ModelState.AddModelError("VehicleId", "Ten pojazd ma już zaplanowany inny AKTYWNY przegląd.");
                }
            }

            if (inspection.IsResultPositive == true)
            {
                inspection.NextInspectionDate = inspection.InspectionDate.AddYears(1);
            }
            else if (inspection.IsResultPositive == false)
            {
                inspection.NextInspectionDate = inspection.InspectionDate.AddDays(14);
            }
            else
            {
                inspection.NextInspectionDate = null;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (inspection.Mileage.HasValue && inspection.Mileage > 0)
                    {
                        if (vehicle.Status == VehicleStatus.Sold)
                        {
                            ModelState.AddModelError("VehicleId", "Nie można dodać przeglądu dla tego pojazdu, ponieważ został sprzedany");
                        }
                        else if (inspection.Mileage.Value > vehicle.CurrentKm)
                        {
                            vehicle.CurrentKm = inspection.Mileage.Value;
                            context.Update(vehicle);
                        }
                    }

                    if (ModelState.IsValid)
                    {
                        context.Update(inspection);
                        await context.SaveChangesAsync();
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!context.Inspections.Any(e => e.Id == inspection.Id))
                        return NotFound();
                    else
                        throw;
                }
            }

            var vehiclesQuery = context.Vehicles.AsQueryable();
            if (!isAdminOrManager)
            {
                vehiclesQuery = vehiclesQuery.Where(v => v.UserId == currentUser.Id || (v.Driver != null && v.Driver.UserId == currentUser.Id));
            }

            var vehicleList = await vehiclesQuery
                .Select(v => new { v.VehicleId, DisplayText = v.LicensePlate ?? (v.Make + " " + v.Model) })
                .ToListAsync();

            ViewBag.VehicleList = new SelectList(vehicleList, "VehicleId", "DisplayText", inspection.VehicleId);
            return View(inspection);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();
            var isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");

            var inspection = await context.Inspections
                .Include(i => i.Vehicle)
                .ThenInclude(v => v!.Driver)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (inspection == null) return NotFound();

            if (!isAdminOrManager && (inspection.Vehicle?.UserId != currentUser.Id && inspection.Vehicle?.Driver?.UserId != currentUser.Id))
            {
                return Forbid();
            }

            return View(inspection);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();
            var isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");

            var inspection = await context.Inspections
                .Include(i => i.Vehicle)
                .ThenInclude(v => v!.Driver)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inspection != null)
            {
                if (!isAdminOrManager && (inspection.Vehicle?.UserId != currentUser.Id && inspection.Vehicle?.Driver?.UserId != currentUser.Id))
                {
                    return Forbid();
                }

                context.Inspections.Remove(inspection);
                await context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}