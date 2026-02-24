using Fleet_Managment_Production.Data;
using Fleet_Managment_Production.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq; // Ważne dla metod LINQ

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

        // GET: Costs
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
        public IActionResult Create()
        {
            // Używamy tych samych metod pomocniczych co w Edit dla spójności
            PopulateVehiclesDropdown();
            // Możesz użyć PopulateManualCostTypesDropdown() jeśli chcesz wykluczyć automatyczne typy, 
            // lub poniższego kodu, jeśli chcesz wszystkie:
            ViewData["CostType"] = new SelectList(Enum.GetValues(typeof(CostType)));

            return View();
        }

        // POST: Costs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Tu miałeś wszystko dobrze (Liters, CurrentOdometer są w Bind)
        public async Task<IActionResult> Create([Bind("Id,VehicleId,Type,Opis,Kwota,Data,Liters,CurrentOdometer,IsFullTank")] Cost cost)
        {
            if (ModelState.IsValid)
            {
                _context.Add(cost);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            PopulateVehiclesDropdown(cost.VehicleId);
            ViewData["CostType"] = new SelectList(Enum.GetValues(typeof(CostType)), cost.Type);
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

            // Blokada edycji kosztów systemowych
            if (cost.Type == CostType.Przegląd || cost.Type == CostType.Ubezpieczenie)
            {
                TempData["ErrorMessage"] = "Nie można edytować kosztów wygenerowanych automatycznie.";
                return RedirectToAction(nameof(Index));
            }

            PopulateVehiclesDropdown(cost.VehicleId);
            PopulateManualCostTypesDropdown(cost.Type); // Używamy helpera, żeby wykluczyć typy systemowe z listy
            return View(cost);
        }

        // POST: Costs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        // UWAGA: Dodałem brakujące pola paliwowe do Bind! (Liters, CurrentOdometer, IsFullTank)
        public async Task<IActionResult> Edit(int id, [Bind("Id,VehicleId,Type,Opis,Kwota,Data,Liters,CurrentOdometer,IsFullTank")] Cost cost)
        {
            if (id != cost.Id)
            {
                return NotFound();
            }

            // Sprawdzamy czy nie próbujemy edytować kosztu systemowego (security check)
            var originalCostType = await _context.Costs
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => c.Type)
                .FirstOrDefaultAsync();

            if (originalCostType == CostType.Przegląd || originalCostType == CostType.Ubezpieczenie)
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

        // Metoda pomocnicza do ładowania listy pojazdów
        private void PopulateVehiclesDropdown(object selectedVehicle = null)
        {
            var vehiclesQuery = _context.Vehicles
                .OrderBy(v => v.LicensePlate) // Sortowanie po tablicy
                .Select(v => new {
                    // TU BYŁ BŁĄD: Zmieniamy v.Id na v.VehicleId
                    Id = v.VehicleId,
                    // TU BYŁ BŁĄD: Zmieniamy v.Brand na v.Make i RegistrationNumber na LicensePlate
                    DisplayText = v.LicensePlate ?? $"{v.Make} {v.Model} (Brak rej.)"
                });

            ViewData["VehicleId"] = new SelectList(vehiclesQuery, "Id", "DisplayText", selectedVehicle);
        }

        // Metoda pomocnicza do ładowania typów kosztów (bez systemowych)
        private void PopulateManualCostTypesDropdown(object selectedType = null)
        {
            var manualTypes = Enum.GetValues(typeof(CostType))
                .Cast<CostType>()
                .Where(t => t != CostType.Przegląd && t != CostType.Ubezpieczenie) // Wykluczamy automatyczne
                .Select(t => new { Value = t, Text = t.ToString() });

            ViewData["CostType"] = new SelectList(manualTypes, "Value", "Text", selectedType);
        }
    }
}