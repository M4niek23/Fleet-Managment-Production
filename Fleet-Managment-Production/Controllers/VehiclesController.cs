using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Fleet_Managment_Production.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Fleet_Managment_Production.Models;
using Microsoft.Build.Framework;

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
        public async Task<IActionResult> Index(string searchString, int? driverId, int? page, VehicleStatus? status)
        {
            int pageSize = 7;
            int pageNumber = page ?? 1;
            var currentUserId = _userManager.GetUserId(User);
            ViewData["CurrentFilter"] = searchString;
            var isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");

            var vehiclesQuery = _context.Vehicles
                .Include(v => v.User)
                .Include(v => v.Driver)
                .AsQueryable();

            if (!isAdminOrManager)
            {
                var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == currentUserId);
                if (driver != null) vehiclesQuery = vehiclesQuery.Where(v => v.DriverId == driver.Id);
                else vehiclesQuery = vehiclesQuery.Where(v => false);
            }

            if (driverId.HasValue)
            {
                vehiclesQuery = vehiclesQuery.Where(v => v.DriverId == driverId.Value);
                var selectedDriver = await _context.Drivers.FindAsync(driverId.Value);
                ViewBag.SelectedDriverName = selectedDriver != null ? $"{selectedDriver.FirstName} {selectedDriver.LastName}" : "Wybranego kierowcy";
            }
            else
            {
                ViewBag.SelectedDriverName = "Wszyscy kierowcy";
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                vehiclesQuery = vehiclesQuery.Where(v =>
                   v.Make.Contains(searchString) ||
                   v.Model.Contains(searchString) ||
                   (v.Make + " " + v.Model).Contains(searchString) ||
                   (v.Model + " " + v.Make).Contains(searchString) ||
                   (v.LicensePlate != null && v.LicensePlate.Contains(searchString)) ||
                   (v.Driver != null && (
                        v.Driver.FirstName.Contains(searchString) ||
                        v.Driver.LastName.Contains(searchString) ||
                        (v.Driver.FirstName + " " + v.Driver.LastName).Contains(searchString) ||
                        (v.Driver.LastName + " " + v.Driver.FirstName).Contains(searchString)
                   ))
                );
            }

            ViewBag.CountAll = await vehiclesQuery.CountAsync();
            ViewBag.CountAvailable = await vehiclesQuery.CountAsync(v => v.Status == VehicleStatus.Available);
            ViewBag.CountInUse = await vehiclesQuery.CountAsync(v => v.Status == VehicleStatus.InUse);
            ViewBag.CountMaintenance = await vehiclesQuery.CountAsync(v => v.Status == VehicleStatus.InMaintenance);
            ViewBag.CountSold = await vehiclesQuery.CountAsync(v => v.Status == VehicleStatus.Sold);

            if (status.HasValue)
            {
                vehiclesQuery = vehiclesQuery.Where(v => v.Status == status.Value);
            }

            ViewBag.CurrentStatus = status;
            ViewBag.CurrentDriverId = driverId;

            var totalItems = await vehiclesQuery.CountAsync();
            var vehiclesList = await vehiclesQuery
                .OrderBy(v => v.Driver != null ? v.Driver.LastName : "")
                .ThenBy(v => v.Driver != null ? v.Driver.FirstName : "")
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var allDrivers = await _context.Drivers.Select(d => new { d.Id, Name = d.FirstName + " " + d.LastName }).ToListAsync();
            ViewBag.DriverList = new SelectList(allDrivers, "Id", "Name", driverId);

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

                int totalDistance = lastEntry.CurrentOdometer.GetValueOrDefault() - firstEntry.CurrentOdometer.GetValueOrDefault();
                double totalLiters = fuelCosts.Skip(1).Sum(c => c.Liters.GetValueOrDefault());

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
            if (_context.Vehicles.Any(v => v.VIN == vehicle.VIN))
            {
                ModelState.AddModelError("VIN", "Pojazd o podanym numerze VIN już istnieje w systemie.");
            }
            if (_context.Vehicles.Any(v => v.LicensePlate == vehicle.LicensePlate))
            {
                ModelState.AddModelError("LicensePlate", "Pojazd o tym numerze rejestracyjnym jest już zarejestrowany.");
            }
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
            if (_context.Vehicles.Any(v => v.LicensePlate == vehicle.LicensePlate))
            {
                ModelState.AddModelError("LicensePlate", "Pojazd o tym numerze rejestracyjnym jest już zarejestrowany.");
            }
            if (_context.Vehicles.Any(v => v.VIN == vehicle.VIN && v.VehicleId != vehicle.VehicleId))
            {
                ModelState.AddModelError("VIN", "Inny pojazd posiada już ten numer VIN.");
            }
            if (id != vehicle.VehicleId) return NotFound();

            var currentVehicle = await _context.Vehicles
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.VehicleId == id);

            if (currentVehicle == null) return NotFound();

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

        private void PopulateUsersDropdown(object? selectedUser = null)
        {
            var usersQuery = _userManager.Users
                .Select(u => new { u.Id, DisplayName = u.UserName })
                .OrderBy(u => u.DisplayName)
                .ToList();

            ViewBag.UserId = new SelectList(usersQuery, "Id", "DisplayName", selectedUser);
        }

        private void PopulateDriversDropdown(object? selectedDriver = null)
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