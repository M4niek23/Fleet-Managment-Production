using System;
using System.Linq;
using System.Threading.Tasks;
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
    public class CostsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public CostsController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Costs
        public async Task<IActionResult> Index(int? page)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");

            var costsQuery = _context.Costs
                .Include(c => c.Vehicle)
                .ThenInclude(v => v.Driver)
                .OrderByDescending(c => c.Data)
                .AsQueryable();

            if (!isAdminOrManager)
            {
                costsQuery = costsQuery.Where(c => c.Vehicle != null && c.Vehicle.Driver != null && c.Vehicle.Driver.UserId == currentUser.Id);
            }

            ViewBag.TotalSum = await costsQuery.SumAsync(c => c.Amount);

            int pageSize = 10;
            int pageNumber = page ?? 1;
            var totalItems = await costsQuery.CountAsync();

            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var costsList = await costsQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(costsList);
        }

        // GET: Costs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var cost = await _context.Costs
                .Include(c => c.Vehicle).ThenInclude(v => v.Driver)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (cost == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
            {
                if (cost.Vehicle?.Driver?.UserId != currentUser.Id) return Forbid();
            }

            return View(cost);
        }

        // GET: Costs/Create
        public async Task<IActionResult> Create()
        {
            await PopulateVehiclesDropdownAsync();
            ViewData["CostType"] = new SelectList(Enum.GetValues(typeof(CostType)));
            return View();
        }

        // POST: Costs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,VehicleId,Type,Description,Amount,Data,Liters,CurrentOdometer,IsFullTank")] Cost cost)
        {
            if (ModelState.IsValid)
            {
                _context.Add(cost);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            await PopulateVehiclesDropdownAsync(cost.VehicleId);
            ViewData["CostType"] = new SelectList(Enum.GetValues(typeof(CostType)), cost.Type);
            return View(cost);
        }

        // GET: Costs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var cost = await _context.Costs.Include(c => c.Vehicle).ThenInclude(v => v.Driver).FirstOrDefaultAsync(c => c.Id == id);
            if (cost == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
            {
                if (cost.Vehicle?.Driver?.UserId != currentUser.Id) return Forbid();
            }

            if (cost.Type == CostType.Przegląd || cost.Type == CostType.Ubezpieczenie)
            {
                TempData["ErrorMessage"] = "Nie można edytować kosztów wygenerowanych automatycznie.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateVehiclesDropdownAsync(cost.VehicleId);
            PopulateManualCostTypesDropdown(cost.Type);
            return View(cost);
        }

        // POST: Costs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,VehicleId,Type,Description,Amount,Data,Liters,CurrentOdometer,IsFullTank")] Cost cost)
        {
            if (id != cost.Id) return NotFound();

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
                    if (!CostExists(cost.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            await PopulateVehiclesDropdownAsync(cost.VehicleId);
            PopulateManualCostTypesDropdown(cost.Type);
            return View(cost);
        }

        // GET: Costs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var cost = await _context.Costs
                .Include(c => c.Vehicle).ThenInclude(v => v.Driver)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (cost == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
            {
                if (cost.Vehicle?.Driver?.UserId != currentUser.Id) return Forbid();
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
            if (cost == null) return NotFound();

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

        private async Task PopulateVehiclesDropdownAsync(object selectedVehicle = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");

            var vehiclesQuery = _context.Vehicles.AsQueryable();

            if (!isAdminOrManager)
            {
                vehiclesQuery = vehiclesQuery.Where(v => v.Driver != null && v.Driver.UserId == currentUser.Id);
            }

            var vehicles = await vehiclesQuery
                .OrderBy(v => v.LicensePlate)
                .Select(v => new {
                    Id = v.VehicleId,
                    DisplayText = v.LicensePlate != null ? v.LicensePlate : $"{v.Make} {v.Model} (Brak rej.)"
                }).ToListAsync();

            ViewData["VehicleId"] = new SelectList(vehicles, "Id", "DisplayText", selectedVehicle);
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