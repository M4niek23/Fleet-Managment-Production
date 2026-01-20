using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Fleet_Managment_Production.Data;
using Fleet_Managment_Production.Models;
using Microsoft.AspNetCore.Authorization;

namespace Fleet_Managment_Production.Controllers
{
    [Authorize]
    public class TripsController : Controller
    {
        private readonly AppDbContext _context;

        public TripsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Trips
        public async Task<IActionResult> Index()
        {
            var trips = _context.Trips.Include(t => t.Driver).Include(t => t.Vehicle);
            return View(await trips.ToListAsync());
        }

        // GET: Trips/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var trip = await _context.Trips
                .Include(t => t.Driver)
                .Include(t => t.Vehicle)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (trip == null) return NotFound();

            return View(trip);
        }

        // GET: Trips/Create
        public IActionResult Create()
        {
            PrepareDropdowns();
            return View();
        }

        // POST: Trips/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,VehicleId,DriverId,StartDate,EndTime,StartLocation,EndLocation,StartLatitude,StartLongitude,EndLatitude,EndLongitude,EstimatedDistanceKm,StartOdometer,EndOdometer,Description,TripType")] Trip trip)
        {
            // 1. Walidacja Ubezpieczenia
            bool hasValidInsurance = await _context.Insurances
                .AnyAsync(i => i.VehicleId == trip.VehicleId &&
                               i.StartDate.Date <= trip.StartDate.Date &&
                               i.ExpiryDate.Date >= trip.StartDate.Date);

            if (!hasValidInsurance)
            {
                ModelState.AddModelError("VehicleId", "Wybrany pojazd nie posiada ważnego ubezpieczenia w dniu rozpoczęcia podróży!");
            }

            // 2. NOWA WALIDACJA: Sprawdzenie ważności przeglądu
            // Sprawdzamy czy jest przegląd pozytywny i czy jego data ważności (NextInspectionDate) jest >= dacie podróży
            bool hasValidInspection = await _context.Inspections
                .AnyAsync(i => i.VehicleId == trip.VehicleId &&
                               i.IsResultPositive == true &&
                               (i.NextInspectionDate != null && i.NextInspectionDate.Value.Date >= trip.StartDate.Date));

            if (!hasValidInspection)
            {
                ModelState.AddModelError("VehicleId", "Wybrany pojazd nie posiada ważnego przeglądu technicznego!");
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
            PrepareDropdowns(trip);
            return View(trip);
        }

        // GET: Trips/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var trip = await _context.Trips.FindAsync(id);
            if (trip == null) return NotFound();

            PrepareDropdowns(trip);
            return View(trip);
        }

        // POST: Trips/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,VehicleId,DriverId,StartDate,EndTime,StartLocation,EndLocation,StartLatitude,StartLongitude,EndLatitude,EndLongitude,EstimatedDistanceKm,StartOdometer,EndOdometer,Description,TripType")] Trip trip)
        {
            if (id != trip.Id) return NotFound();

            // 1. Walidacja Ubezpieczenia
            bool hasValidInsurance = await _context.Insurances
                .AnyAsync(i => i.VehicleId == trip.VehicleId &&
                               i.StartDate.Date <= trip.StartDate.Date &&
                               i.ExpiryDate.Date >= trip.StartDate.Date);

            if (!hasValidInsurance)
            {
                ModelState.AddModelError("VehicleId", "Pojazd nie posiada ważnego ubezpieczenia w wybranym terminie!");
            }

            // 2. NOWA WALIDACJA: Sprawdzenie ważności przeglądu
            bool hasValidInspection = await _context.Inspections
                .AnyAsync(i => i.VehicleId == trip.VehicleId &&
                               i.IsResultPositive == true &&
                               (i.NextInspectionDate != null && i.NextInspectionDate.Value.Date >= trip.StartDate.Date));

            if (!hasValidInspection)
            {
                ModelState.AddModelError("VehicleId", "Pojazd nie posiada ważnego przeglądu technicznego w wybranym terminie!");
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
            PrepareDropdowns(trip);
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

        private void PrepareDropdowns(Trip? trip = null)
        {
            ViewData["DriverId"] = new SelectList(_context.Drivers, "Id", "LastName", trip?.DriverId);
            ViewData["VehicleId"] = new SelectList(_context.Vehicles.Select(v => new {
                Id = v.VehicleId,
                Description = $"{v.Make} {v.Model} ({v.LicensePlate})"
            }), "Id", "Description", trip?.VehicleId);
        }
    }
}