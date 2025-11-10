using Fleet_Managment_Production.Data;
using Fleet_Managment_Production.Models;
using Fleet_Managment_Production.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Fleet_Managment_Production.Controllers
{
    public class InspectionsController : Controller
    {
        private readonly AppDbContext _context;

        public InspectionsController(AppDbContext context)
        {
            _context = context;
        }

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

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var inspection = await _context.Inspections
                .Include(i => i.Vehicle)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (inspection == null) return NotFound();

            return View(inspection);
        }

        public IActionResult Create()
        {
            ViewData["VehicleId"] = new SelectList(_context.Vehicles, "VehicleId", "LicensePlate");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,InspectionDate,Description,Mileage,Cost,VehicleId,IsResultPositive")] Inspection inspection)
        {
            var existingUpcoming = await _context.Inspections
                .FirstOrDefaultAsync(i => i.VehicleId == inspection.VehicleId && 
                                          i.InspectionDate >= DateTime.Today &&
                                          i.IsActive == true);

            if (existingUpcoming != null)
            {
                ModelState.AddModelError("VehicleId", "Ten pojazd ma jeszcze ważny przegląd. Jeśli poprzedni wygaśnie samoczynnie lub ręcznie do zdezaktywuje, można dodać kolejny");

            }

            if (inspection.IsResultPositive == false)
            {
                inspection.NextInspectionDate = inspection.InspectionDate.AddDays(14);
            }else
            {
                inspection.NextInspectionDate = null;
            }

            if (ModelState.IsValid)
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

                _context.Add(inspection);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["VehicleId"] = new SelectList(_context.Vehicles, "VehicleId", "LicensePlate", inspection.VehicleId);
            return View(inspection);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var inspection = await _context.Inspections.FindAsync(id);
            if (inspection == null) return NotFound();
            ViewData["VehicleId"] = new SelectList(_context.Vehicles, "VehicleId", "LicensePlate", inspection.VehicleId);
            return View(inspection);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,InspectionDate,Description,Mileage,Cost,VehicleId,IsResultPositive")] Inspection inspection)
        {
            if (id != inspection.Id) return NotFound();

            // --- LOGIKA BIZNESOWA 1 (Edycja): WALIDACJA "JEDEN AKTYWNY" ---
            // Sprawdzamy, czy próbujemy ustawić ten przegląd jako aktywny/nadchodzący,
            // podczas gdy INNY (o innym Id) już taki istnieje.
            if (inspection.IsActive == true && inspection.InspectionDate.Date >= DateTime.Today)
            {
                var existingUpcoming = await _context.Inspections
                    .FirstOrDefaultAsync(i => i.VehicleId == inspection.VehicleId &&
                                              i.InspectionDate.Date >= DateTime.Today &&
                                              i.IsActive == true &&
                                              i.Id != inspection.Id); // Wykluczamy samych siebie

                if (existingUpcoming != null)
                {
                    ModelState.AddModelError("VehicleId", "Ten pojazd ma już zaplanowany inny AKTYWNY przegląd.");
                }
            }
            // --- KONIEC LOGIKI 1 ---

            // --- LOGIKA BIZNESOWA 2: WYNIK NEGATYWNY = 14 DNI ---
            if (inspection.IsResultPositive == false)
            {
                inspection.NextInspectionDate = inspection.InspectionDate.AddDays(14);
            }
            else
            {
                inspection.NextInspectionDate = null;
            }
            // --- KONIEC LOGIKI 2 ---

            if (ModelState.IsValid)
            {
                try
                {
                    // --- LOGIKA BIZNESOWA 3: SYNCHRONIZACJA PRZEBIEGU ---
                    if (inspection.Mileage.HasValue && inspection.Mileage > 0)
                    {
                        var vehicleToUpdate = await _context.Vehicles.FindAsync(inspection.VehicleId);
                        if (vehicleToUpdate != null && inspection.Mileage.Value > vehicleToUpdate.CurrentKm)
                        {
                            vehicleToUpdate.CurrentKm = inspection.Mileage.Value;
                            _context.Update(vehicleToUpdate);
                        }
                    }
                    // --- KONIEC LOGIKI 3 ---

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

            ViewData["VehicleId"] = new SelectList(_context.Vehicles, "VehicleId", "LicensePlate", inspection.VehicleId);
            return View(inspection);
        }

        public async Task<IActionResult> Delete(int? id)
        {
           if(id == null) return NotFound();
           var inspection = await _context.Inspections
                .Include(i => i.VehicleId)
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