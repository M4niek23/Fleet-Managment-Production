using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Fleet_Managment_Production.Data;
using Fleet_Managment_Production.Models;

namespace Fleet_Managment_Production.Controllers
{
    public class DriversController : Controller
    {
        private readonly AppDbContext _context;

        public DriversController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Drivers
        public async Task<IActionResult> Index()
        {
            var drivers = await _context.Drivers
                .Include(d => d.Vehicles)
                .Include(d => d.User) 
                .ToListAsync();
            return View(drivers);
        }

        // GET: Drivers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var driver = await _context.Drivers
                .Include(d => d.Vehicles)
                .Include(d => d.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (driver == null) return NotFound();

            return View(driver);
        }

        // GET: Drivers/Create
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email");
            return View();
        }

        // POST: Drivers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FirstName,LastName,Pesel,LicenseCategory,PhoneNumber,UserId,Email")] Driver driver)
        {
            if (!string.IsNullOrEmpty(driver.UserId))
            {
                var user = await _context.Users.FindAsync(driver.UserId);
                if (user != null) driver.Email = user.Email;
            }

            ModelState.Remove("User"); 

            if (ModelState.IsValid)
            {
                if (await _context.Drivers.AnyAsync(d => d.Pesel == driver.Pesel))
                {
                    ModelState.AddModelError("Pesel", "Ten PESEL już istnieje w bazie.");
                    ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", driver.UserId);
                    return View(driver);
                }

                _context.Add(driver);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", driver.UserId);
            return View(driver);
        }

        // GET: Drivers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var driver = await _context.Drivers.FindAsync(id);
            if (driver == null) return NotFound();

            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", driver.UserId);
            return View(driver);
        }

        // POST: Drivers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,Pesel,LicenseCategory,PhoneNumber,UserId,Email")] Driver driver)
        {
            if (id != driver.Id) return NotFound();

            if (!string.IsNullOrEmpty(driver.UserId))
            {
                var user = await _context.Users.FindAsync(driver.UserId);
                if (user != null) driver.Email = user.Email;
            }

            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(driver);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Drivers.Any(e => e.Id == driver.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", driver.UserId);
            return View(driver);
        }

        // GET: Drivers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var driver = await _context.Drivers
                .Include(d => d.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (driver == null) return NotFound();

            return View(driver);
        }

        // POST: Drivers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var driver = await _context.Drivers.FindAsync(id);
            if (driver != null)
            {
                _context.Drivers.Remove(driver);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}