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
    public class DriversController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;
        public DriversController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Drivers
        public async Task<IActionResult> Index(string searchString, string sortOrder)
        {
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["CurrentFilter"] = searchString;
            var driversQuery = _context.Drivers
                .Include(d => d.Vehicles)
                .Include(d => d.User)
                .AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                var lowerSearch = searchString.ToLower();
                driversQuery = driversQuery.Where(d =>
                       d.FirstName.ToLower().Contains(lowerSearch) ||
                       d.LastName.ToLower().Contains(lowerSearch) ||
                       (d.FirstName + " " + d.LastName).ToLower().Contains(lowerSearch) ||
                       (d.LastName + " " + d.FirstName).ToLower().Contains(lowerSearch)
                    );
            }
            driversQuery = sortOrder switch
            {
                "name_desc" => driversQuery.OrderByDescending(d => d.LastName).ThenByDescending(d => d.FirstName),
                _ => driversQuery.OrderBy(d => d.LastName).ThenBy(d => d.FirstName),
            };
            return View(await driversQuery.ToListAsync());
        }

        // GET: Drivers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var driver = await _context.Drivers
                .Include(d => d.Vehicles)              
                .Include(d => d.Trips)                
                    .ThenInclude(t => t.Vehicle) 
                .FirstOrDefaultAsync(m => m.Id == id);

            if (driver == null) return NotFound();

            driver.Trips = driver.Trips.OrderByDescending(t => t.StartDate).ToList();

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
        public async Task<IActionResult> Create([Bind("Id,FirstName,LastName,Pesel,SelectedCategories,LicenseCategories,PhoneNumber,UserId,Email")] Driver driver)
        {
            if (!string.IsNullOrEmpty(driver.UserId))
            {
                var user = await _context.Users.FindAsync(driver.UserId);
                if (user != null) driver.Email = user.Email;
            }

            ModelState.Remove("User");
            if (driver.SelectedCategories != null && driver.SelectedCategories.Any())
            {
                driver.LicenseCategories = string.Join(", ", driver.SelectedCategories);
            }

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
            if (!string.IsNullOrEmpty(driver.LicenseCategories))
            {
                driver.SelectedCategories = driver.LicenseCategories
                    .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => Enum.TryParse<LicenseCategory>(c, out var parsedEnum) ? parsedEnum : (LicenseCategory?)null)
                    .Where(c => c.HasValue)
                    .Select(c => c.Value)
                    .ToList();
            }

            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", driver.UserId);
            return View(driver);
        }

        // POST: Drivers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,Pesel,SelectedCategories,LicenseCategories,PhoneNumber,UserId,Email")] Driver driver)
        {
            if (id != driver.Id) return NotFound();

            if (!string.IsNullOrEmpty(driver.UserId))
            {
                var user = await _context.Users.FindAsync(driver.UserId);
                if (user != null) driver.Email = user.Email;
            }

            ModelState.Remove("User");

            if (driver.SelectedCategories != null && driver.SelectedCategories.Any())
            {
                driver.LicenseCategories = string.Join(", ", driver.SelectedCategories);
            }
            else
            {
                driver.LicenseCategories = null;
            }

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