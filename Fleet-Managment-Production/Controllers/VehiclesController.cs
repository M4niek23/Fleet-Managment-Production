using System;
using System.Linq;
using System.Threading.Tasks;
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
            var vehicles = await _context.Vehicles
                .Include(v => v.User)
                .Include(v => v.Driver) // Dodano ładowanie danych kierowcy
                .ToListAsync();
            return View(vehicles);
        }

        // GET: Vehicles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var vehicle = await _context.Vehicles
                .Include(v => v.User)
                .Include(v => v.Driver) // Dodano ładowanie danych kierowcy
                .Include(v => v.Insurances)
                .FirstOrDefaultAsync(m => m.VehicleId == id);

            if (vehicle == null)
                return NotFound();

            return View(vehicle);
        }

        // GET: Vehicles/Create
        public IActionResult Create()
        {
            PopulateUsersDropdown();
            PopulateDriversDropdown(); // Ładowanie listy kierowców
            return View();
        }

        // POST: Vehicles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Upewnij się, że DriverId jest w Bind
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

            // W razie błędu przywróć obie listy
            PopulateUsersDropdown(vehicle.UserId);
            PopulateDriversDropdown(vehicle.DriverId);
            return View(vehicle);
        }

        // GET: Vehicles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null)
                return NotFound();

            PopulateUsersDropdown(vehicle.UserId);
            PopulateDriversDropdown(vehicle.DriverId); // Ładowanie listy z zaznaczonym obecnym kierowcą
            return View(vehicle);
        }

        // POST: Vehicles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Dodano DriverId do Bind
        public async Task<IActionResult> Edit(int id, [Bind("VehicleId,Status,Make,Model,FuelType,ProductionYear,LicensePlate,VIN,CurrentKm,UserId,DriverId")] Vehicle vehicle)
        {
            if (id != vehicle.VehicleId)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(vehicle);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VehicleExists(vehicle.VehicleId))
                        return NotFound();
                    else
                        throw;
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
            if (id == null)
                return NotFound();

            var vehicle = await _context.Vehicles
                .Include(v => v.User)
                .Include(v => v.Driver) // Dodano, aby widzieć kogo usuwamy wraz z autem
                .FirstOrDefaultAsync(m => m.VehicleId == id);

            if (vehicle == null)
                return NotFound();

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

        // Istniejąca metoda do Użytkowników
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

        // NOWA metoda do Kierowców
        private void PopulateDriversDropdown(object selectedDriver = null)
        {
            var driversQuery = _context.Drivers
                .Select(d => new
                {
                    d.Id, // Używamy 'Id', bo tak masz w modelu Driver
                    FullName = d.LastName + " " + d.FirstName + (d.PESEL != null ? " (" + d.PESEL + ")" : "")
                })
                .OrderBy(d => d.FullName)
                .ToList();

            // ZMIANA TUTAJ: Drugi parametr to "Id", bo tak nazywa się pole w obiekcie wyżej
            ViewBag.DriverId = new SelectList(driversQuery, "Id", "FullName", selectedDriver);
        }
    }
}