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
    public class InsuranceController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public InsuranceController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Insurance
        public async Task<IActionResult> Index(int? id, string searchString, int? activePage, int? historyPage, string tab)
        {
            ViewData["CurrentFilter"] = searchString;
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");

            var vehiclesQuery = _context.Vehicles.AsQueryable();
            if (!isAdminOrManager)
            {
                vehiclesQuery = vehiclesQuery.Where(v => v.Driver != null && v.Driver.UserId == currentUser.Id);
            }

            var vehicles = await vehiclesQuery.ToListAsync();
            ViewBag.VehicleList = new SelectList(vehicles.Select(v => new {
                v.VehicleId,
                DisplayText = v.LicensePlate ?? $"{v.Make} {v.Model}"
            }), "VehicleId", "DisplayText", id);

            IQueryable<Insurance> insurancesQuery = _context.Insurances.Include(i => i.Vehicle);

            if (!isAdminOrManager)
            {
                insurancesQuery = insurancesQuery.Where(i => i.Vehicle.Driver.UserId == currentUser.Id);
            }

            if (id.HasValue)
            {
                insurancesQuery = insurancesQuery.Where(i => i.VehicleId == id.Value);
                var selected = vehicles.FirstOrDefault(v => v.VehicleId == id.Value);
                ViewBag.VehicleRegistration = selected?.LicensePlate ?? "Wybranego pojazdu";
                ViewBag.VehicleId = id.Value;
            }
            else
            {
                ViewBag.VehicleRegistration = "Wszystkich pojazdów";
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                var lowerSearch = searchString.ToLower();
                insurancesQuery = insurancesQuery.Where(i =>
                    (i.PolicyNumber != null && i.PolicyNumber.ToLower().Contains(lowerSearch)) ||
                    (i.Vehicle.LicensePlate != null && i.Vehicle.LicensePlate.ToLower().Contains(lowerSearch))
                );
            }

            int pageSize = 7;
            int actPageNumber = activePage ?? 1;
            int histPageNumber = historyPage ?? 1;

            var activeQuery = insurancesQuery.Where(i => i.IsCurrent).OrderByDescending(i => i.ExpiryDate);
            var historyQuery = insurancesQuery.Where(i => !i.IsCurrent).OrderByDescending(i => i.ExpiryDate);

            ViewBag.TotalSum = await activeQuery.SumAsync(i => (double?)i.Cost) ?? 0;

            ViewBag.ActiveCurrentPage = actPageNumber;
            ViewBag.ActiveTotalPages = (int)Math.Ceiling(await activeQuery.CountAsync() / (double)pageSize);
            ViewBag.ActiveCount = await activeQuery.CountAsync();

            ViewBag.HistoryCurrentPage = histPageNumber;
            ViewBag.HistoryTotalPages = (int)Math.Ceiling(await historyQuery.CountAsync() / (double)pageSize);
            ViewBag.HistoryCount = await historyQuery.CountAsync();

            ViewBag.ActiveInsurances = await activeQuery.Skip((actPageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            ViewBag.HistoryInsurances = await historyQuery.Skip((histPageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.ActiveTab = tab ?? "active";

            return View();
        }

        // GET: Insurance/History
        public async Task<IActionResult> History(int? id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");

            IQueryable<Insurance> historyQuery = _context.Insurances
                .Include(i => i.Vehicle)
                .Where(i => !i.IsCurrent); // Tylko historyczne

            if (!isAdminOrManager)
            {
                historyQuery = historyQuery.Where(i => i.Vehicle.Driver.UserId == currentUser.Id);
            }

            if (id.HasValue)
            {
                historyQuery = historyQuery.Where(i => i.VehicleId == id.Value);
                var vehicle = await _context.Vehicles.FindAsync(id);
                ViewBag.VehicleName = vehicle?.LicensePlate ?? "Pojazdu";
            }

            return View(await historyQuery.OrderByDescending(i => i.ExpiryDate).ToListAsync());
        }

        // GET: Insurance/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");

            var insurance = await _context.Insurances
                .Include(i => i.Vehicle).ThenInclude(v => v.Driver)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (insurance == null) return NotFound();

            if (!isAdminOrManager && insurance.Vehicle?.Driver?.UserId != currentUser.Id)
                return Forbid();

            return View(insurance);
        }

        // GET: Insurance/Create
        public async Task<IActionResult> Create(int? vehicleId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");

            var vehiclesQuery = _context.Vehicles.AsQueryable();
            if (!isAdminOrManager)
                vehiclesQuery = vehiclesQuery.Where(v => v.Driver != null && v.Driver.UserId == currentUser.Id);

            ViewBag.VehicleList = new SelectList(await vehiclesQuery.ToListAsync(), "VehicleId", "LicensePlate", vehicleId);
            return View(new Insurance { StartDate = DateTime.Today, ExpiryDate = DateTime.Today.AddYears(1), IsCurrent = true });
        }

        // POST: Insurance/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PolicyNumber,InsurareName,StartDate,ExpiryDate,Cost,VehicleId,IsCurrent,HasOc,HasAssistance,AcScope,HasNNW")] Insurance insurance)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");

            var vehicle = await _context.Vehicles.Include(v => v.Driver).FirstOrDefaultAsync(v => v.VehicleId == insurance.VehicleId);
            if (vehicle == null || (!isAdminOrManager && vehicle.Driver?.UserId != currentUser.Id))
                return Forbid();

            ModelState.Remove("Vehicle");
            if (ModelState.IsValid)
            {
                if (insurance.IsCurrent)
                {
                    // Deaktywacja poprzednich polis dla tego auta
                    var activeInsurances = await _context.Insurances
                        .Where(i => i.VehicleId == insurance.VehicleId && i.IsCurrent)
                        .ToListAsync();
                    foreach (var active in activeInsurances) active.IsCurrent = false;
                }

                _context.Add(insurance);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index), new { id = insurance.VehicleId });
            }
            return View(insurance);
        }

        // GET: Insurance/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");

            var insurance = await _context.Insurances
                .Include(i => i.Vehicle).ThenInclude(v => v.Driver)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (insurance == null) return NotFound();

            if (!isAdminOrManager && insurance.Vehicle?.Driver?.UserId != currentUser.Id)
                return Forbid();

            var vehiclesQuery = _context.Vehicles.AsQueryable();
            if (!isAdminOrManager)
                vehiclesQuery = vehiclesQuery.Where(v => v.Driver != null && v.Driver.UserId == currentUser.Id);

            ViewBag.VehicleList = new SelectList(await vehiclesQuery.ToListAsync(), "VehicleId", "LicensePlate", insurance.VehicleId);
            return View(insurance);
        }

        // POST: Insurance/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,PolicyNumber,InsurareName,StartDate,ExpiryDate,Cost,VehicleId,IsCurrent,HasOc,HasAssistance,AcScope,HasNNW")] Insurance insurance)
        {
            if (id != insurance.Id) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");

            var vehicle = await _context.Vehicles.Include(v => v.Driver).FirstOrDefaultAsync(v => v.VehicleId == insurance.VehicleId);
            if (vehicle == null || (!isAdminOrManager && vehicle.Driver?.UserId != currentUser.Id))
                return Forbid();

            ModelState.Remove("Vehicle");
            if (ModelState.IsValid)
            {
                try
                {
                    if (insurance.IsCurrent)
                    {
                        var others = await _context.Insurances
                            .Where(i => i.VehicleId == insurance.VehicleId && i.IsCurrent && i.Id != insurance.Id)
                            .ToListAsync();
                        foreach (var o in others) o.IsCurrent = false;
                    }
                    _context.Update(insurance);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Insurances.Any(e => e.Id == insurance.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index), new { id = insurance.VehicleId });
            }
            return View(insurance);
        }

        // GET: Insurance/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");

            var insurance = await _context.Insurances
                .Include(i => i.Vehicle).ThenInclude(v => v.Driver)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (insurance == null) return NotFound();

            if (!isAdminOrManager && insurance.Vehicle?.Driver?.UserId != currentUser.Id)
                return Forbid();

            return View(insurance);
        }

        // POST: Insurance/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");

            var insurance = await _context.Insurances
                .Include(i => i.Vehicle).ThenInclude(v => v.Driver)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (insurance != null)
            {
                if (!isAdminOrManager && insurance.Vehicle?.Driver?.UserId != currentUser.Id)
                    return Forbid();

                int? vehicleId = insurance.VehicleId;
                _context.Insurances.Remove(insurance);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index), new { id = vehicleId });
            }

            return RedirectToAction(nameof(Index));
        }
    }
}