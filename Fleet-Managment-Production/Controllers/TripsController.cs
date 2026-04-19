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
    public class TripsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;


        public TripsController(AppDbContext context,UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Trips
        // GET: Trips
        public async Task<IActionResult> Index(int? filterDriverId, int? filterVehicleId, int? page)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");

            // Zapamiętujemy wybrane filtry dla widoku i paginacji
            ViewData["CurrentDriverFilter"] = filterDriverId;
            ViewData["CurrentVehicleFilter"] = filterVehicleId;

            var tripsQuery = _context.Trips
                .Include(t => t.Driver)
                .Include(t => t.Vehicle)
                .OrderByDescending(t => t.StartDate)
                .AsQueryable();

            if (!isAdminOrManager)
            {
                tripsQuery = tripsQuery.Where(t =>
                    (t.Driver != null && t.Driver.UserId == currentUser.Id) ||
                    (t.Vehicle.Driver != null && t.Vehicle.Driver.UserId == currentUser.Id));
            }

            // --- FILTROWANIE ---
            if (filterDriverId.HasValue)
            {
                tripsQuery = tripsQuery.Where(t => t.DriverId == filterDriverId.Value);
            }
            if (filterVehicleId.HasValue)
            {
                tripsQuery = tripsQuery.Where(t => t.VehicleId == filterVehicleId.Value);
            }

            // --- PRZYGOTOWANIE LIST ROZWIJANYCH DO FILTROWANIA ---
            var driversQuery = _context.Drivers.AsQueryable();
            var vehiclesQuery = _context.Vehicles.AsQueryable();

            if (!isAdminOrManager)
            {
                driversQuery = driversQuery.Where(d => d.UserId == currentUser.Id);
                vehiclesQuery = vehiclesQuery.Where(v => v.Driver != null && v.Driver.UserId == currentUser.Id);
            }

            ViewData["DriversList"] = new SelectList(await driversQuery.Select(d => new {
                d.Id,
                FullName = d.FirstName + " " + d.LastName
            }).ToListAsync(), "Id", "FullName", filterDriverId);

            ViewData["VehiclesList"] = new SelectList(await vehiclesQuery.Select(v => new {
                Id = v.VehicleId,
                Description = $"{v.Make} {v.Model} ({v.LicensePlate})"
            }).ToListAsync(), "Id", "Description", filterVehicleId);


            int pageSize = 7;
            int pageNumber = page ?? 1;
            var totalItems = await tripsQuery.CountAsync();

            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var tripsList = await tripsQuery.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            return View(tripsList);
        }

        // GET: Trips/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var trip = await _context.Trips
                .Include(t => t.Driver)
                .Include(t => t.Vehicle).ThenInclude(v => v.Driver)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (trip == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
            {
                if (trip.Driver?.UserId != currentUser.Id && trip.Vehicle?.Driver?.UserId != currentUser.Id)
                    return Forbid();
            }
            return View(trip);
        }

        // GET: Trips/Create
        public async Task<IActionResult> Create()
        {
            await PrepareDropdownsAsync();
            return View();
        }

        // POST: Trips/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,VehicleId,DriverId,StartDate,EndTime,StartLocation,EndLocation,StartLatitude,StartLongitude,EndLatitude,EndLongitude,EstimatedDistanceKm,StartOdometer,EndOdometer,Description,TripType")] Trip trip)
        {
            var vehicle = await _context.Vehicles.FindAsync(trip.VehicleId);
            if (vehicle != null && vehicle.Status == VehicleStatus.Sold)
            {
                ModelState.AddModelError("VehicleId", "Ten pojazd został sprzedany, nie można przypisać mu nowej trasy.");
            }
            bool hasValidInsurance = await _context.Insurances
                .AnyAsync(i => i.VehicleId == trip.VehicleId &&
                               i.StartDate.Date <= trip.StartDate.Date &&
                               i.ExpiryDate.Date >= trip.StartDate.Date);

            if (!hasValidInsurance)
            {
                ModelState.AddModelError("VehicleId", "Wybrany pojazd nie posiada ważnego ubezpieczenia w dniu rozpoczęcia podróży!");
            }

            bool hasValidInspection = await _context.Inspections
                .AnyAsync(i => i.VehicleId == trip.VehicleId &&
                               i.IsResultPositive == true &&
                               (i.NextInspectionDate != null && i.NextInspectionDate.Value.Date >= trip.StartDate.Date));

            if (!hasValidInspection)
            {
                ModelState.AddModelError("VehicleId", "Wybrany pojazd nie posiada ważnego przeglądu technicznego!");
            }
            if (trip.EndOdometer.HasValue)
            {
                if (trip.EndOdometer.Value < trip.StartOdometer)
                {
                    ModelState.AddModelError("EndOdometer", "Stan licznika końcowego nie może być mniejszy niż przebieg początkowy!");
                }
            }
            if (ModelState.IsValid)
            {
                _context.Add(trip);

                if (trip.EndOdometer.HasValue)
                {
                    await UpdateVehicleOdometer(trip.VehicleId, trip.EndOdometer.Value);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            await PrepareDropdownsAsync(trip);
            return View(trip);
        }

        // GET: Trips/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var trip = await _context.Trips.FindAsync(id);
            if (trip == null) return NotFound();

            await PrepareDropdownsAsync(trip);
            return View(trip);
        }

        // POST: Trips/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,VehicleId,DriverId,StartDate,EndTime,StartLocation,EndLocation,StartLatitude,StartLongitude,EndLatitude,EndLongitude,EstimatedDistanceKm,StartOdometer,EndOdometer,Description,TripType")] Trip trip)
        {
            if (id != trip.Id) return NotFound();
            var vehicleCheck = await _context.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.VehicleId == trip.VehicleId);
            if (vehicleCheck != null && vehicleCheck.Status == VehicleStatus.Sold)
            {
                ModelState.AddModelError("VehicleId", "Ten pojazd został sprzedany, nie można przypisać mu trasy.");
            }
            bool hasValidInsurance = await _context.Insurances
                .AnyAsync(i => i.VehicleId == trip.VehicleId &&
                               i.StartDate.Date <= trip.StartDate.Date &&
                               i.ExpiryDate.Date >= trip.StartDate.Date);

            if (!hasValidInsurance)
            {
                ModelState.AddModelError("VehicleId", "Pojazd nie posiada ważnego ubezpieczenia w wybranym terminie!");
            }

            bool hasValidInspection = await _context.Inspections
                .AnyAsync(i => i.VehicleId == trip.VehicleId &&
                               i.IsResultPositive == true &&
                               (i.NextInspectionDate != null && i.NextInspectionDate.Value.Date >= trip.StartDate.Date));

            if (!hasValidInspection)
            {
                ModelState.AddModelError("VehicleId", "Pojazd nie posiada ważnego przeglądu technicznego w wybranym terminie!");
            }
            if (trip.EndOdometer.HasValue)
            {
                if (trip.EndOdometer.Value < trip.StartOdometer)
                {
                    ModelState.AddModelError("EndOdometer", "Stan licznika końcowego nie może być mniejszy niż przebieg początkowy!");
                }
            }
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(trip);

                    if (trip.EndOdometer.HasValue)
                    {
                        await UpdateVehicleOdometer(trip.VehicleId, trip.EndOdometer.Value);
                    }

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TripExists(trip.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            await PrepareDropdownsAsync(trip);
            return View(trip);
        }

        // GET: Trips/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var trip = await _context.Trips
                .Include(t => t.Driver)
                .Include(t => t.Vehicle)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (trip == null) return NotFound();

            return View(trip);
        }

        // POST: Trips/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip != null) _context.Trips.Remove(trip);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TripExists(int id) => _context.Trips.Any(e => e.Id == id);

        private async Task UpdateVehicleOdometer(int vehicleId, int newOdometer)
        {
            var vehicle = await _context.Vehicles.FindAsync(vehicleId);
            if (vehicle != null && newOdometer > vehicle.CurrentKm)
            {
                vehicle.CurrentKm = newOdometer;
                _context.Update(vehicle);
            }
        }
        private async Task PrepareDropdownsAsync(Trip? trip = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");

            var driversQuery = _context.Drivers.AsQueryable();
            var vehiclesQuery = _context.Vehicles
                    .Where(v => v.Status != VehicleStatus.Sold || (trip != null && v.VehicleId == trip.VehicleId))
                    .AsQueryable();

            if (!isAdminOrManager)
            {
                driversQuery = driversQuery.Where(d => d.UserId == currentUser.Id);
                vehiclesQuery = vehiclesQuery.Where(v => v.Driver != null && v.Driver.UserId == currentUser.Id);
            }

            ViewData["DriverId"] = new SelectList(await driversQuery.Select(d => new {
                d.Id,
                FullName = d.FirstName + " " + d.LastName
            }).ToListAsync(), "Id", "FullName", trip?.DriverId);

            ViewData["VehicleId"] = new SelectList(await vehiclesQuery.Select(v => new {
                Id = v.VehicleId,
                Description = $"{v.Make} {v.Model} ({v.LicensePlate})"
            }).ToListAsync(), "Id", "Description", trip?.VehicleId);
        }
        // GET: Trips/GetVehicleOdometer/5
        [HttpGet]
        public async Task<IActionResult> GetVehicleOdometer(int? id)
        {
            if (id == null) return Json(new { odometer = 0 });

            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle != null)
            {
                return Json(new { odometer = vehicle.CurrentKm });
            }
            return Json(new { odometer = 0 });
        }
    }
}