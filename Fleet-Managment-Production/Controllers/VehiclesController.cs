using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Fleet_Managment_Production.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Fleet_Managment_Production.Models;

namespace Fleet_Managment_Production.Controllers
{
    [Authorize]
    public class VehiclesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public VehiclesController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Vehicles
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.Vehicles
                .Include(v => v.User)
                .Include(v => v.Driver); // Jest OK
            return View(await appDbContext.ToListAsync());
        }

        // GET: Vehicles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var vehicle = await _context.Vehicles
                // Dołączamy powiązane dane (Relacje)
                .Include(v => v.Trips).ThenInclude(t => t.Driver) // Pobierz trasy wraz z kierowcami
                .Include(v => v.Inspections)                      // Pobierz przeglądy
                .Include(v => v.Insurances)                       // Pobierz ubezpieczenia
                .Include(v => v.Costs)                            // Pobierz koszty
                .Include(v => v.Services)                           // Pobierz Serwis
                .FirstOrDefaultAsync(m => m.VehicleId == id);

            if (vehicle == null) return NotFound();

            // Sortujemy listy, aby najnowsze wpisy były na górze
            vehicle.Trips = vehicle.Trips.OrderByDescending(t => t.StartDate).ToList();
            vehicle.Inspections = vehicle.Inspections.OrderByDescending(i => i.InspectionDate).ToList();
            vehicle.Insurances = vehicle.Insurances.OrderByDescending(i => i.ExpiryDate).ToList();
            vehicle.Costs = vehicle.Costs.OrderByDescending(c => c.Data).ToList();
            vehicle.Services = vehicle.Services.OrderByDescending(s => s.ActualEndDate).ToList();

            return View(vehicle);
        }

        // GET: Vehicles/Create
        public IActionResult Create()
        {
            PopulateUsersDropdown();
            PopulateDriversDropdown(); 
            return View();
        }

        // POST: Vehicles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("VehicleId,Status,Make,Model,FuelType,ProductionYear,LicensePlate,VIN,CurrentKm,UserId,DriverId")] Vehicle vehicle)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(vehicle.UserId))
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                        vehicle.UserId = user.Id;
                }

                _context.Vehicles.Add(vehicle);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            PopulateUsersDropdown(vehicle.UserId);
            PopulateDriversDropdown(vehicle.DriverId); 
            return View(vehicle);
        }

        // GET: Vehicles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var hasActiveService = await _context.Services.AnyAsync(s => s.VehicleId == id && s.ActualEndDate == null); var vehicle = await _context.Vehicles.FindAsync(id);
            ViewBag.IsStatusLocked = hasActiveService;
            if (vehicle == null) return NotFound();

            PopulateUsersDropdown(vehicle.UserId);
            PopulateDriversDropdown(vehicle.DriverId);
            return View(vehicle);
        }

        // POST: Vehicles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("VehicleId,Status,Make,Model,FuelType,ProductionYear,LicensePlate,VIN,CurrentKm,UserId,DriverId")] Vehicle vehicle)
        {
            if (id != vehicle.VehicleId) return NotFound();

            var currenVehilce = await _context.Vehicles
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.VehicleId == id);
            if (currenVehilce == null) return NotFound();
            if (currenVehilce.Status != vehicle.Status)
            {
                bool hasActiveService = await _context.Services
                    .AnyAsync(s => s.VehicleId == id && s.ActualEndDate == null);
                if (hasActiveService)
                {
                    ModelState.AddModelError("Status", "Nie można zmienić statusu pojazdu, dopóki serwis nie zostanie zakończony (brak daty zakończenia w modul Serwis).");
                    PopulateUsersDropdown(vehicle.UserId);
                    PopulateDriversDropdown(vehicle.DriverId);
                    return View(vehicle);
                }
            }
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(vehicle);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VehicleExists(vehicle.VehicleId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            PopulateUsersDropdown(vehicle.UserId);
            PopulateDriversDropdown(vehicle.DriverId); 
            return View(vehicle);
        }

        // GET: Vehicles/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var vehicle = await _context.Vehicles
                .Include(v => v.User)
                .Include(v => v.Driver) 
                .FirstOrDefaultAsync(m => m.VehicleId == id);

            if (vehicle == null) return NotFound();

            return View(vehicle);
        }

        // POST: Vehicles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle != null)
            {
                _context.Vehicles.Remove(vehicle);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool VehicleExists(int id)
        {
            return _context.Vehicles.Any(e => e.VehicleId == id);
        }


        private void PopulateUsersDropdown(object selectedUser = null)
        {
            var usersQuery = _userManager.Users
                .Select(u => new { u.Id, DisplayName = u.UserName })
                .OrderBy(u => u.DisplayName)
                .ToList();

            if (usersQuery.Count == 0)
            {
                ViewBag.UserId = new SelectList(new[]
                {
                    new { Id = "", DisplayName = "Brak dostępnych użytkowników" }
                }, "Id", "DisplayName", selectedUser);
            }
            else
            {
                ViewBag.UserId = new SelectList(usersQuery, "Id", "DisplayName", selectedUser);
            }
        }

        private void PopulateDriversDropdown(object selectedDriver = null)
        {
            var driversQuery = _context.Drivers
                .Select(d => new
                {
                    d.Id,
                    FullName = d.FirstName + " " + d.LastName + " (" + d.Pesel + ")"
                })
                .OrderBy(d => d.FullName)
                .ToList();

            ViewBag.DriverId = new SelectList(driversQuery, "Id", "FullName", selectedDriver);
        }
    }
}