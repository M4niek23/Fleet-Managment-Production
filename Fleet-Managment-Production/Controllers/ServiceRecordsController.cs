using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Fleet_Managment_Production.Data;
using Fleet_Managment_Production.Models;

namespace Fleet_Managment_Production.Controllers
{
    public class ServiceRecordsController : Controller
    {
        private readonly AppDbContext _context;

        public ServiceRecordsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var records = await _context.ServiceRecords.Include(s => s.Vehicle).ToListAsync();
            return View(records);
        }
        // GET: ServiceRecords/Create
        public IActionResult Create()
        {
            // Przekazujemy listę pojazdów do dropdowna w widoku
            ViewBag.VehicleId = new SelectList(_context.Vehicles, "Id", "LicensePlate");
            return View();
        }

        // POST: ServiceRecords/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceRecord record)
        {
            // Usuwamy walidację dla pola Vehicle, bo chcemy walidować tylko ID
            ModelState.Remove("Vehicle");

            if (ModelState.IsValid)
            {
                var vehicle = await _context.Vehicles.FindAsync(record.VehicleId);

                if (vehicle == null)
                {
                    ModelState.AddModelError("", "Nie znaleziono wybranego pojazdu.");
                }
                // Upewnij się, czy porównujesz do stringa "W serwisie" czy do Enuma!
                else if (vehicle.Status != VehicleStatus.InMaintenance)
                {
                    ModelState.AddModelError("", "BŁĄD: Możesz dodać naprawę tylko dla pojazdu, który ma status 'W serwisie'.");
                }
                else
                {
                    _context.Add(record);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index)); // Przekieruj do listy napraw
                }
            }

            ViewBag.VehicleId = new SelectList(_context.Vehicles, "Id", "LicensePlate", record.VehicleId);
            return View(record);
        }
        // GET: ServiceRecords/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var serviceRecord = await _context.ServiceRecords
                .Include(s => s.Vehicle)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (serviceRecord == null) return NotFound();

            return View(serviceRecord);
        }

        // GET: ServiceRecords/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var serviceRecord = await _context.ServiceRecords.FindAsync(id);
            if (serviceRecord == null) return NotFound();

            ViewBag.VehicleId = new SelectList(_context.Vehicles, "Id", "LicensePlate", serviceRecord.VehicleId);
            return View(serviceRecord);
        }

        // POST: ServiceRecords/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ServiceRecord record)
        {
            if (id != record.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(record);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Details), new { id = record.Id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.ServiceRecords.Any(e => e.Id == record.Id)) return NotFound();
                    else throw;
                }
            }
            ViewBag.VehicleId = new SelectList(_context.Vehicles, "Id", "LicensePlate", record.VehicleId);
            return View(record);
        }

        // GET: ServiceRecords/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var serviceRecord = await _context.ServiceRecords
                .Include(s => s.Vehicle)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (serviceRecord == null) return NotFound();

            return View(serviceRecord);
        }

        // POST: ServiceRecords/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var serviceRecord = await _context.ServiceRecords.FindAsync(id);
            if (serviceRecord != null)
            {
                _context.ServiceRecords.Remove(serviceRecord);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index", "Vehicles");
        }

    }
}