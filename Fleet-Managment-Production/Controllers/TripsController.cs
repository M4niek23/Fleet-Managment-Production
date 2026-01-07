using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            if (ModelState.IsValid)
            {
                _context.Add(trip);

                // Jeśli podano licznik końcowy przy tworzeniu -> aktualizuj pojazd
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

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(trip);

                    // Aktualizacja przebiegu pojazdu
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