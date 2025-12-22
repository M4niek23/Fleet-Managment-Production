using Fleet_Managment_Production.Data;
using Fleet_Managment_Production.Models;
using Fleet_Managment_Production.Models.CostsType;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Fleet_Managment_Production.Controllers
{
    [Authorize]
    public class CostsController : Controller
    {
        private readonly AppDbContext _context;

        public CostsController(AppDbContext context)
        {
            _context = context;
        }

        //GET: Costs
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.Costs.Include(c => c.Vehicle);
            return View(await appDbContext.ToListAsync());
        }

        // GET: Costs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cost = await _context.Costs
                .Include(c => c.Vehicle)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (cost == null)
            {
                return NotFound();
            }

            return View(cost);
        }
        // GET: Costs/Create
        public IActionResult Create(int? vehicleId)
        {
            PopulateVehiclesDropdown(vehicleId);
            PopulateManualCostTypesDropdown();

            var model = new Cost();
            if (vehicleId.HasValue)
            {
                model.VehicleId = vehicleId.Value;
            }

            return View(model);
        }
        // POST: Costs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Type,Opis,Kwota,Data,VehicleId")] Cost cost)
        {
            // Walidacja, aby upewnić się, że nikt nie próbuje dodać automatycznego kosztu
            if (cost.Type == CostType.Przegląd || cost.Type == CostType.Ubezpieczenie)
            {
                ModelState.AddModelError("Type", "Nie można ręcznie dodać kosztu typu 'Przegląd' lub 'Ubezpieczenie'.");
            }

            // Sprawdzamy, czy model jest poprawny (łącznie z naszym błędem)
            if (ModelState.IsValid)
            {
                _context.Add(cost);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Jeśli model nie jest poprawny, ponownie wypełniamy listy rozwijane
            PopulateVehiclesDropdown(cost.VehicleId);
            PopulateManualCostTypesDropdown(cost.Type);
            return View(cost);
        }

        // GET: Costs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cost = await _context.Costs.FindAsync(id);
            if (cost == null)
            {
                return NotFound();
            }

            // BLOKADA: Nie pozwalamy edytować kosztów automatycznych
            if (cost.Type == CostType.Przegląd || cost.Type == CostType.Ubezpieczenie)
            {
                TempData["ErrorMessage"] = "Nie można edytować kosztów wygenerowanych automatycznie.";
                return RedirectToAction(nameof(Index));
            }

            PopulateVehiclesDropdown(cost.VehicleId);
            PopulateManualCostTypesDropdown(cost.Type);
            return View(cost);
        }

        // POST: Costs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Type,Opis,Kwota,Data,VehicleId")] Cost cost)
        {
            if (id != cost.Id)
            {
                return NotFound();
            }

            // BLOKADA: Podwójne sprawdzenie na wypadek próby obejścia
            var originalCost = await _context.Costs.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (originalCost.Type == CostType.Przegląd || originalCost.Type == CostType.Ubezpieczenie)
            {
                TempData["ErrorMessage"] = "Nie można edytować kosztów wygenerowanych automatycznie.";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cost);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CostExists(cost.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            PopulateVehiclesDropdown(cost.VehicleId);
            PopulateManualCostTypesDropdown(cost.Type);
            return View(cost);
        }

        // GET: Costs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cost = await _context.Costs
                .Include(c => c.Vehicle)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (cost == null)
            {
                return NotFound();
            }

            if (cost.Type == CostType.Przegląd || cost.Type == CostType.Ubezpieczenie)
            {
                TempData["ErrorMessage"] = "Nie można usunąć kosztów wygenerowanych automatycznie. Usuń powiązaną inspekcję lub ubezpieczenie.";
                return RedirectToAction(nameof(Index));
            }

            return View(cost);
        }

        // POST: Costs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cost = await _context.Costs.FindAsync(id);
            if (cost == null)
            {
                return NotFound();
            }

            if (cost.Type == CostType.Przegląd || cost.Type == CostType.Ubezpieczenie)
            {
                TempData["ErrorMessage"] = "Nie można usunąć kosztów wygenerowanych automatycznie.";
                return RedirectToAction(nameof(Index));
            }

            _context.Costs.Remove(cost);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CostExists(int id)
        {
            return _context.Costs.Any(e => e.Id == id);
        }


        private void PopulateVehiclesDropdown(object selectedVehicle = null)
        {
            // Pobieramy pojazdy do listy rozwijanej (używam LicensePlate, możesz zmienić na Make/Model)
            var vehiclesQuery = _context.Vehicles
                                .OrderBy(v => v.LicensePlate)
                                .Select(v => new {
                                    v.VehicleId,
                                    DisplayText = v.LicensePlate ?? $"{v.Make} {v.Model}" // Pokaż rejestrację, a jeśli brak, to Markę+Model
                                });
            ViewData["VehicleId"] = new SelectList(vehiclesQuery, "VehicleId", "DisplayText", selectedVehicle);
        }

        private void PopulateManualCostTypesDropdown(object selectedType = null)
        {
            var manualTypes = Enum.GetValues(typeof(CostType))
                .Cast<CostType>()
                .Where(t => t != CostType.Przegląd && t != CostType.Ubezpieczenie)
                .Select(t => new { Value = (int)t, Text = t.ToString() });

            ViewData["CostType"] = new SelectList(manualTypes, "Value", "Text", selectedType);
        }
    }
}
