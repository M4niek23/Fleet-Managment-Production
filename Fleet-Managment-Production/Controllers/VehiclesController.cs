using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Fleet_Managment_Production.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Fleet_Managment_Production.Models;

namespace Fleet_Managment_Production.Controllers
{
    [Authorize]
    public class VehiclesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public VehiclesController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Vehicles
        public async Task<IActionResult> Index(string searchString, int? vehicleId)
        {
            var currentUserId = _userManager.GetUserId(User);
            ViewData["CurrentFilter"] = searchString;

            var vehiclesQuery = _context.Vehicles
                .Include(v => v.User)
                .Include(v => v.Driver)
                .AsQueryable();

            if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
            {
                var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == currentUserId);
                if (driver != null)
                {
                    vehiclesQuery = vehiclesQuery.Where(v => v.DriverId == driver.Id);
                }else
                {
                    vehiclesQuery = vehiclesQuery.Where(v => false);
                }
            }
            if (vehicleId.HasValue)
            {
                vehiclesQuery = vehiclesQuery.Where(v => v.VehicleId == vehicleId.Value);

            }
            if (!string.IsNullOrEmpty(searchString))
            {
                var searchLower = searchString.ToLower();

                vehiclesQuery = vehiclesQuery.Where(v =>
                (v.Make != null && v.Make.ToLower().Contains(searchLower)) ||
                (v.Model != null && v.Model.ToLower().Contains(searchLower)) ||
                (v.LicensePlate != null && v.LicensePlate.ToLower().Contains(searchLower)) ||
                (v.VIN != null && v.VIN.ToLower().Contains(searchLower)) ||
                (v.Driver != null && v.Driver.FirstName.ToLower().Contains(searchLower)) ||
                (v.Driver != null && v.Driver.LastName.ToLower().Contains(searchLower))
                );                  
            }

            var vehiclesList = await vehiclesQuery.ToListAsync();

            var dropdownQuery = _context.Vehicles.AsQueryable();
            if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
            {
                var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == currentUserId);
                if(driver != null)
                {
                    dropdownQuery = dropdownQuery.Where(v => v.DriverId == driver.Id);
                }else
                {
                    dropdownQuery = dropdownQuery.Where(v => false);
                }
                
            }
            var allVehiclesForDropdown = await _context.Vehicles.Include(v => v.Driver).ToListAsync();
            var vehiclesSelectList = allVehiclesForDropdown.Select(v => new
            {
                VehicleId = v.VehicleId,
                DisplayText = v.Driver != null
            ? $"{v.Driver.FirstName} {v.Driver.LastName}"
            : $"[Brak kierowcy] {v.Make} {v.Model} ({v.LicensePlate})"
            }).ToList();

            ViewBag.VehicleList = new SelectList(vehiclesSelectList, "VehicleId", "DisplayText");
            return View(vehiclesList);
        }

        // GET: Vehicles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var vehicle = await _context.Vehicles
                .Include(v => v.Trips).ThenInclude(t => t.Driver)
                .Include(v => v.Inspections)
                .Include(v => v.Insurances)
                .Include(v => v.Costs)
                .Include(v => v.Services)
                .Include(v => v.Driver)
                .FirstOrDefaultAsync(m => m.VehicleId == id);

            if (vehicle == null) return NotFound();

            double avgConsumption = 0;
            var fuelCosts = vehicle.Costs
                .Where(c => c.Type == CostType.Paliwo && c.Liters.HasValue && c.CurrentOdometer.HasValue)
                .OrderBy(c => c.CurrentOdometer)
                .ToList();

            if (fuelCosts.Count >= 2)
            {
                var firstEntry = fuelCosts.First();
                var lastEntry = fuelCosts.Last();

                int totalDistance = lastEntry.CurrentOdometer.Value - firstEntry.CurrentOdometer.Value;

                double totalLiters = fuelCosts.Skip(1).Sum(c => c.Liters.Value);

                if (totalDistance > 0)
                {
                    avgConsumption = (totalLiters / totalDistance) * 100;
                }
            }
            ViewBag.AvgConsumption = avgConsumption;

            vehicle.Trips = vehicle.Trips.OrderByDescending(t => t.StartDate).ToList();
            vehicle.Inspections = vehicle.Inspections.OrderByDescending(i => i.InspectionDate).ToList();
            vehicle.Insurances = vehicle.Insurances.OrderByDescending(i => i.ExpiryDate).ToList();
            vehicle.Costs = vehicle.Costs.OrderByDescending(c => c.Data).ToList();
            vehicle.Services = vehicle.Services.OrderByDescending(s => s.ActualEndDate).ToList();

            return View(vehicle);
        }

        // GET: Vehicles/Create
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Create()
        {
            PopulateUsersDropdown();
            PopulateDriversDropdown();
            return View();
        }

        // POST: Vehicles/Create
        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("VehicleId,Status,Make,Model,FuelType,ProductionYear,LicensePlate,VIN,CurrentKm,UserId,DriverId")] Vehicle vehicle)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(vehicle.UserId))
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                        vehicle.UserId = user.Id;
                }

                _context.Vehicles.Add(vehicle);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            PopulateUsersDropdown(vehicle.UserId);
            PopulateDriversDropdown(vehicle.DriverId);
            return View(vehicle);
        }
        [Authorize(Roles = "Admin,Manager")]
        // GET: Vehicles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null) return NotFound();

            var hasActiveService = await _context.Services.AnyAsync(s => s.VehicleId == id && s.ActualEndDate == null);
            ViewBag.IsStatusLocked = hasActiveService;

            PopulateUsersDropdown(vehicle.UserId);
            PopulateDriversDropdown(vehicle.DriverId);
            return View(vehicle);
        }
        [Authorize(Roles = "Admin,Manager")]
        // POST: Vehicles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("VehicleId,Status,Make,Model,FuelType,ProductionYear,LicensePlate,VIN,CurrentKm,UserId,DriverId")] Vehicle vehicle)
        {
            if (id != vehicle.VehicleId) return NotFound();

            var currentVehicle = await _context.Vehicles
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.VehicleId == id);

            if (currentVehicle == null) return NotFound();

            // Blokada zmiany statusu przy aktywnym serwisie
            if (currentVehicle.Status != vehicle.Status)
            {
                bool hasActiveService = await _context.Services
                    .AnyAsync(s => s.VehicleId == id && s.ActualEndDate == null);

                if (hasActiveService)
                {
                    ModelState.AddModelError("Status", "Nie można zmienić statusu pojazdu, dopóki serwis nie zostanie zakończony.");
                    PopulateUsersDropdown(vehicle.UserId);
                    PopulateDriversDropdown(vehicle.DriverId);
                    return View(vehicle);
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(vehicle);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VehicleExists(vehicle.VehicleId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            PopulateUsersDropdown(vehicle.UserId);
            PopulateDriversDropdown(vehicle.DriverId);
            return View(vehicle);
        }

        // GET: Vehicles/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var vehicle = await _context.Vehicles
                .Include(v => v.User)
                .Include(v => v.Driver)
                .FirstOrDefaultAsync(m => m.VehicleId == id);

            if (vehicle == null) return NotFound();

            return View(vehicle);
        }

        // POST: Vehicles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle != null)
            {
                _context.Vehicles.Remove(vehicle);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool VehicleExists(int id)
        {
            return _context.Vehicles.Any(e => e.VehicleId == id);
        }

        private void PopulateUsersDropdown(object selectedUser = null)
        {
            var usersQuery = _userManager.Users
                .Select(u => new { u.Id, DisplayName = u.UserName })
                .OrderBy(u => u.DisplayName)
                .ToList();

            ViewBag.UserId = new SelectList(usersQuery, "Id", "DisplayName", selectedUser);
        }

        private void PopulateDriversDropdown(object selectedDriver = null)
        {
            var driversQuery = _context.Drivers
                .Select(d => new
                {
                    d.Id,
                    FullName = d.FirstName + " " + d.LastName + " (" + d.Pesel + ")"
                })
                .OrderBy(d => d.FullName)
                .ToList();

            ViewBag.DriverId = new SelectList(driversQuery, "Id", "FullName", selectedDriver);
        }
    }
}