using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            var drivers = _context.Drivers.Include(d => d.User);
            return View(await drivers.ToListAsync());
        }

        // GET: Drivers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var driver = await _context.Drivers
                .Include(d => d.User)
                .Include(d => d.Vehicles)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (driver == null) return NotFound();

            return View(driver);
        }

        // GET: Drivers/Create
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email");

            // Przekazujemy wszystkie możliwe enumy do widoku
            ViewBag.AllCategories = Enum.GetValues(typeof(LicenseCategory)).Cast<LicenseCategory>().ToList();

            return View();
        }

        // POST: Drivers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Driver driver)
        {
            // Model Binder sam spróbuje powiązać zaznaczone checkboxy z listą driver.LicenseCategories

            if (ModelState.IsValid)
            {
                _context.Add(driver);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", driver.UserId);
            ViewBag.AllCategories = Enum.GetValues(typeof(LicenseCategory)).Cast<LicenseCategory>().ToList();
            return View(driver);
        }

        // GET: Drivers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var driver = await _context.Drivers.FindAsync(id);
            if (driver == null) return NotFound();

            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", driver.UserId);
            ViewBag.AllCategories = Enum.GetValues(typeof(LicenseCategory)).Cast<LicenseCategory>().ToList();

            return View(driver);
        }

        // POST: Drivers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Driver driver)
        {
            if (id != driver.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(driver);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Drivers.Any(e => e.Id == id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", driver.UserId);
            ViewBag.AllCategories = Enum.GetValues(typeof(LicenseCategory)).Cast<LicenseCategory>().ToList();
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
            if (driver != null) _context.Drivers.Remove(driver);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}