using Fleet_Managment_Production.Data;
using Fleet_Managment_Production.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fleet_Managment_Production.Controllers
{
    public class ServicesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public ServicesController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Services
        public async Task<IActionResult> Index()
        {
            var services = await _context.Services
                .Include(s => s.Vehicle)
                .OrderByDescending(s => s.EntryDate)
                .ToListAsync();
            return View(services);
        }

        // GET: Services/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var service = await _context.Services
                .Include(s => s.Vehicle)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (service == null) return NotFound();

            return View(service);
        }

        // GET: Services/Create
        public IActionResult Create()
        {
            PopulateVehiclesDropdown();
            return View();

        }

        // POST: Services/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,VehicleId,Description,Cost,EntryDate,PlannedEndDate,ActualEndDate")] Service service)
        {
            var vehicle = await _context.Vehicles.FindAsync(service.VehicleId);
            if (vehicle == null) return NotFound();
            if(vehicle.Status != VehicleStatus.InMaintenance)
            {
                ModelState.AddModelError("", "Nie można oddać do samochodu do serwisu, ponieważ ma inny status niż 'W serwsie'. Zmień status i dodaj pojazd.");
                PopulateVehiclesDropdown(service.VehicleId);
                return View(service);
            }

            if (ModelState.IsValid)
            {
                _context.Add(service);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            PopulateVehiclesDropdown(service.VehicleId);
            return View(service);
        }

        // GET: Services/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var service = await _context.Services
                .Include(s => s.Vehicle)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (service == null)
            {
                return NotFound();
            }
            PopulateVehiclesDropdown(service.VehicleId);
            return View(service);
        }

        // POST: Services/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,VehicleId,Description,Cost,EntryDate,PlannedEndDate,ActualEndDate")] Service service)
        {
            if (id != service.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (service.ActualEndDate.HasValue)
                    {
                        var vehicle = await _context.Vehicles.FindAsync(service.VehicleId);
                        if (vehicle != null && vehicle.Status == VehicleStatus.InMaintenance)
                        {
                            vehicle.Status = VehicleStatus.Available;
                            _context.Update(vehicle);
                        }
                    }

                    _context.Update(service);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServiceExists(service.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewBag.VehicleId = new SelectList(_context.Vehicles, "VehicleId", "Make", service.VehicleId);
            return View(service);
        }

        // GET: Services/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var service = await _context.Services
                .Include(s => s.Vehicle)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (service == null)
            {
                return NotFound();
            }

            return View(service);
        }

        // POST: Services/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service != null)
            {
                _context.Services.Remove(service);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ServiceExists(int id)
        {
            return _context.Services.Any(e => e.Id == id);
        }
        private void PopulateVehiclesDropdown(object selectedVehicle = null)
        {
            var vehiclesQuery = _context.Vehicles
                  .Where(v => v.Status == VehicleStatus.InMaintenance)
                  .Select(v => new
                  {
                      v.VehicleId,
                      DisplayName = v.Make + " " + v.Model + " (" + v.LicensePlate + ")"
                  })
                  .OrderBy(v => v.DisplayName)
                  .ToList();
            if (!vehiclesQuery.Any())
            {
                var emptyList = new List<object>
                {
                new { VehicleId = "", DisplayName = "Brak pojazdów o statusie 'W serwisie' - zmień status auta we flocie" }
                };
                ViewBag.VehicleId = new SelectList(emptyList, "VehicleId", "DisplayName");

            }
            else
            {
                ViewBag.VehicleId = new SelectList(vehiclesQuery, "VehicleId", "DisplayName", selectedVehicle);
            }        
        }
    }
}
