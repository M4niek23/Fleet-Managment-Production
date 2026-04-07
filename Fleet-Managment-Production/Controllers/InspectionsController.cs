using Fleet_Managment_Production.Data;
using Fleet_Managment_Production.Models;
using Fleet_Managment_Production.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.EntityFrameworkCore;

namespace Fleet_Managment_Production.Controllers
{
    public class InspectionsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public InspectionsController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int? id)
        {
            if (!_context.Vehicles.Any())
            {
                return View("NoVehicles");
            }

            var allInspections = await _context.Inspections
                                                .Include(i => i.Vehicle)
                                                .OrderByDescending(i => i.InspectionDate)
                                                .ToListAsync();

            var viewModel = new InspectionsViewModel
            {
                ActiveInspections = allInspections
                                     .Where(i => i.IsResultPositive != false &&
                                                (i.NextInspectionDate == null || i.NextInspectionDate >= DateTime.Today))
                                     .OrderBy(i => i.InspectionDate) 
                                     .ToList(),
                HistoricalInspections = allInspections
                                     .Where(i => i.IsResultPositive != false &&
                                                 i.NextInspectionDate < DateTime.Today)
                                     .OrderByDescending(i => i.InspectionDate)
                                     .ToList(),

                NegativeInspections = allInspections
                                     .Where(i => i.IsResultPositive == false)
                                     .OrderByDescending(i => i.InspectionDate)
                                     .ToList()
            };
            return View(viewModel);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var inspection = await _context.Inspections
                .Include(i => i.Vehicle)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (inspection == null) return NotFound();

            return View(inspection);
        }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,InspectionDate,Description,Mileage,Cost,VehicleId,IsResultPositive,IsActive")] Inspection inspection)
        {
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

            // --- NOWA LOGIKA: Pobranie pojazdu i weryfikacja przebiegu ---
            var vehicleToUpdate = await _context.Vehicles.FindAsync(inspection.VehicleId);

            if (vehicleToUpdate != null && inspection.Mileage.HasValue)
            {
                // Jeśli wpisany przebieg jest mniejszy niż aktualny w pojeździe, rzucamy błąd
                if (inspection.Mileage.Value < vehicleToUpdate.CurrentKm)
                {
                    ModelState.AddModelError("Mileage", $"Przebieg nie może być mniejszy niż aktualny stan licznika pojazdu ({vehicleToUpdate.CurrentKm} km).");
                }
            }
            // -------------------------------------------------------------

            if (ModelState.IsValid)
            {
                // Aktualizacja przebiegu pojazdu (wykona się tylko wtedy, gdy walidacja przejdzie pomyślnie)
                if (vehicleToUpdate != null && inspection.Mileage.HasValue && inspection.Mileage.Value > vehicleToUpdate.CurrentKm)
                {
                    vehicleToUpdate.CurrentKm = inspection.Mileage.Value;
                    _context.Update(vehicleToUpdate);
                }

                _context.Add(inspection);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,InspectionDate,Description,Mileage,Cost,VehicleId,IsResultPositive,IsActive")] Inspection inspection)
        {
            if (id != inspection.Id) return NotFound();

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

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var inspection = await _context.Inspections
                .Include(i => i.Vehicle)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (inspection == null) return NotFound();
            return View(inspection);
        }

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