using Fleet_Managment_Production.Data;
using Fleet_Managment_Production.Models;
using Fleet_Managment_Production.ViewModels; // Upewnij się, że ten using jest poprawny
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Fleet_Managment_Production.Controllers
{
    public class InspectionsController : Controller
    {
        private readonly AppDbContext _context;

        public InspectionsController(AppDbContext context)
        {
            _context = context;
        }

        // --- Metoda Index (bez zmian) ---
        public async Task<IActionResult> Index()
        {
            var allInspections = await _context.Inspections
                                                .Include(i => i.Vehicle)
                                                .OrderByDescending(i => i.InspectionDate)
                                                .ToListAsync();

            var viewModel = new InspectionsViewModel
            {
                UpcomingInspections = allInspections
                                            .Where(i => i.IsActive == true && i.InspectionDate.Date >= DateTime.Today)
                                            .ToList(),
                HistoricalInspections = allInspections
                                            .Where(i => i.IsActive == false || i.InspectionDate.Date < DateTime.Today)
                                            .ToList()
            };

            return View(viewModel);
        }

        // --- Metoda Details (bez zmian) ---
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var inspection = await _context.Inspections
                .Include(i => i.Vehicle)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (inspection == null) return NotFound();

            return View(inspection);
        }

        // --- Metoda [GET] Create (bez zmian) ---
        public async Task<IActionResult> Create(int? vehicleId)
        {
            var vehicleList = await _context.Vehicles
                .Select(v => new
                {
                    v.VehicleId,
                    DisplayText = v.LicensePlate ?? (v.Make + " " + v.Model)
                })
                .ToListAsync();

            ViewBag.VehicleList = new SelectList(vehicleList, "VehicleId", "DisplayText", vehicleId);

            return View(new Inspection
            {
                InspectionDate = DateTime.Today,
                IsActive = true
            });
        }

        // ========== POPRAWIONA METODA [POST] CREATE ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,InspectionDate,Description,Mileage,Cost,VehicleId,IsResultPositive,IsActive")] Inspection inspection)
        {
            // Logika walidacji "jeden aktywny"
            if (inspection.IsActive == true && inspection.InspectionDate.Date >= DateTime.Today)
            {
                var existingUpcoming = await _context.Inspections
                    .FirstOrDefaultAsync(i => i.VehicleId == inspection.VehicleId &&
                                              i.InspectionDate.Date >= DateTime.Today &&
                                              i.IsActive == true);

                if (existingUpcoming != null)
                {
                    ModelState.AddModelError("VehicleId", "Ten pojazd ma już zaplanowany AKTYWNY przegląd.");
                }
            }

            // --- POPRAWIONA LOGIKA DATY (3 STANY) ---
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
            // --- KONIEC POPRAWKI ---

            if (ModelState.IsValid)
            {
                // Logika synchronizacji przebiegu
                if (inspection.Mileage.HasValue && inspection.Mileage > 0)
                {
                    var vehicleToUpdate = await _context.Vehicles.FindAsync(inspection.VehicleId);
                    if (vehicleToUpdate != null && inspection.Mileage.Value > vehicleToUpdate.CurrentKm)
                    {
                        vehicleToUpdate.CurrentKm = inspection.Mileage.Value;
                        _context.Update(vehicleToUpdate);
                    }
                }

                _context.Add(inspection);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Ponowne ładowanie listy do ViewBag, gdy walidacja zawiedzie
            var vehicleList = await _context.Vehicles
                .Select(v => new
                {
                    v.VehicleId,
                    DisplayText = v.LicensePlate ?? (v.Make + " " + v.Model)
                })
                .ToListAsync();

            ViewBag.VehicleList = new SelectList(vehicleList, "VehicleId", "DisplayText", inspection.VehicleId);

            return View(inspection);
        }

        // --- Metoda [GET] Edit (bez zmian) ---
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var inspection = await _context.Inspections.FindAsync(id);
            if (inspection == null) return NotFound();

            var vehicleList = await _context.Vehicles
                .Select(v => new
                {
                    v.VehicleId,
                    DisplayText = v.LicensePlate ?? (v.Make + " " + v.Model)
                })
                .ToListAsync();

            ViewBag.VehicleList = new SelectList(vehicleList, "VehicleId", "DisplayText", inspection.VehicleId);
            return View(inspection);
        }

        // --- Metoda [POST] Edit (bez zmian, już była poprawna) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,InspectionDate,Description,Mileage,Cost,VehicleId,IsResultPositive,IsActive")] Inspection inspection)
        {
            if (id != inspection.Id) return NotFound();

            // Logika walidacji "jeden aktywny"
            if (inspection.IsActive == true && inspection.InspectionDate.Date >= DateTime.Today)
            {
                var existingUpcoming = await _context.Inspections
                    .FirstOrDefaultAsync(i => i.VehicleId == inspection.VehicleId &&
                                              i.InspectionDate.Date >= DateTime.Today &&
                                              i.IsActive == true &&
                                              i.Id != inspection.Id);

                if (existingUpcoming != null)
                {
                    ModelState.AddModelError("VehicleId", "Ten pojazd ma już zaplanowany inny AKTYWNY przegląd.");
                }
            }

            // Logika "wynik negatywny = 14 dni" (POPRAWNA)
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
                    // Logika synchronizacji przebiegu
                    if (inspection.Mileage.HasValue && inspection.Mileage > 0)
                    {
                        var vehicleToUpdate = await _context.Vehicles.FindAsync(inspection.VehicleId);
                        if (vehicleToUpdate != null && inspection.Mileage.Value > vehicleToUpdate.CurrentKm)
                        {
                            vehicleToUpdate.CurrentKm = inspection.Mileage.Value;
                            _context.Update(vehicleToUpdate);
                        }
                    }

                    _context.Update(inspection);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Inspections.Any(e => e.Id == inspection.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            // Ponowne ładowanie listy do ViewBag
            var vehicleList = await _context.Vehicles
                .Select(v => new
                {
                    v.VehicleId,
                    DisplayText = v.LicensePlate ?? (v.Make + " " + v.Model)
                })
                .ToListAsync();

            ViewBag.VehicleList = new SelectList(vehicleList, "VehicleId", "DisplayText", inspection.VehicleId);
            return View(inspection);
        }

        // --- Metoda [GET] Delete (bez zmian) ---
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var inspection = await _context.Inspections
                .Include(i => i.Vehicle)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (inspection == null) return NotFound();
            return View(inspection);
        }

        // --- Metoda [POST] Delete (bez zmian) ---
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var inspection = await _context.Inspections.FindAsync(id);
            if (inspection != null)
            {
                _context.Inspections.Remove(inspection);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}