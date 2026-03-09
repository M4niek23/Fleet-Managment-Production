using Fleet_Managment_Production.Data;
using Fleet_Managment_Production.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Fleet_Managment_Production.Controllers
{
    public class InsuranceController : Controller
    {
        private readonly AppDbContext _context;

        public InsuranceController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? id)
        {
            // Sprawdzamy czy są jakiekolwiek pojazdy
            var vehicles = await _context.Vehicles.ToListAsync();
            if (!vehicles.Any())
            {
                return View("NoVehicles");
            }

            // Tworzymy listę pojazdów do filtra (ViewBag.VehicleList)
            var vehicleSelectList = vehicles.Select(v => new {
                v.VehicleId,
                DisplayText = v.LicensePlate ?? $"{v.Make} {v.Model}"
            });
            ViewBag.VehicleList = new SelectList(vehicleSelectList, "VehicleId", "DisplayText", id);

            // Bazowe zapytanie ubezpieczeń - od razu dołączamy dane pojazdu (Include)
            IQueryable<Insurance> insurancesQuery = _context.Insurances.Include(i => i.Vehicle);

            // Jeśli id zostało przekazane -> filtrujemy dla konkretnego pojazdu
            if (id.HasValue)
            {
                insurancesQuery = insurancesQuery.Where(i => i.VehicleId == id.Value);

                var selectedVehicle = vehicles.FirstOrDefault(v => v.VehicleId == id.Value);
                ViewBag.VehicleRegistration = selectedVehicle?.LicensePlate ?? "Brak Rejestracji";
                ViewBag.VehicleId = id.Value;
            }
            else
            {
                // Jeśli id jest null -> wyświetlamy wszystkie
                ViewBag.VehicleRegistration = "Wszystkie pojazdy";
                ViewBag.VehicleId = null; // Przekazujemy null, żeby przycisk "Dodaj" nie wymuszał pojazdu
            }

            // Sortujemy i pobieramy dane
            var insurances = await insurancesQuery
                .OrderByDescending(i => i.IsCurrent)
                .ThenByDescending(i => i.ExpiryDate)
                .ToListAsync();

            return View(insurances);
        }


        public async Task<IActionResult> Create(int? vehicleId)
        {
            var vehicleList = await _context.Vehicles
                                    .Select(v => new
                                    {
                                        v.VehicleId,
                                        DisplayText = v.LicensePlate ?? (v.Make + " " + v.Model)
                                    })
                                    .ToListAsync();

            ViewBag.VehicleList = new SelectList(vehicleList, "VehicleId", "DisplayText", vehicleId);

            return View(new Insurance
            {
                StartDate = DateTime.Today,
                ExpiryDate = DateTime.Today.AddYears(1),
                IsCurrent = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
                [Bind("PolicyNumber,InsurareName,StartDate,ExpiryDate,Cost,VehicleId,HasOc,HasAssistance,AcScope,IsCurrent,HasNNW")]
            Insurance insurance)
        {
            ModelState.Remove("Vehicle");
            if (ModelState.IsValid)
            {
                if (insurance.IsCurrent)
                {
                    var otherInsurances = await _context.Insurances
                        .Where(i => i.VehicleId == insurance.VehicleId && i.IsCurrent)
                        .ToListAsync();

                    foreach (var oldIns in otherInsurances)
                    {
                        oldIns.IsCurrent = false;
                    }
                }

                _context.Add(insurance);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index), new { id = insurance.VehicleId });
            }


            var vehicleList = await _context.Vehicles
                                    .Select(v => new
                                    {
                                        v.VehicleId,
                                        DisplayText = v.LicensePlate ?? (v.Make + " " + v.Model)
                                    })
                                    .ToListAsync();

            ViewBag.VehicleList = new SelectList(vehicleList, "VehicleId", "DisplayText", insurance.VehicleId);


            return View(insurance);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var insurance = await _context.Insurances
                .Include(i => i.Vehicle)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (insurance == null)
            {
                return NotFound();
            }
            return View(insurance);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var insurance = await _context.Insurances.FindAsync(id);
            if (insurance == null)
            {
                return NotFound();
            }

            var vehicleList = await _context.Vehicles
                                    .Select(v => new
                                    {
                                        v.VehicleId,
                                        DisplayText = v.LicensePlate ?? (v.Make + " " + v.Model)
                                    })
                                    .ToListAsync();

            ViewBag.VehicleList = new SelectList(vehicleList, "VehicleId", "DisplayText", insurance.VehicleId);

            return View(insurance);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("Id,PolicyNumber,InsurareName,StartDate,ExpiryDate,Cost,VehicleId,HasOc,HasAssistance,AcScope,IsCurrent,HasNNW")]
            Insurance insurance)
        {
            if (id != insurance.Id)
            {
                return NotFound();
            }


            if (ModelState.IsValid)
            {
                try
                {
                    if (insurance.IsCurrent)
                    {
                        var otherInsurances = await _context.Insurances
                            .Where(i => i.VehicleId == insurance.VehicleId && i.IsCurrent && i.Id != insurance.Id) // Wykluczamy samą siebie
                            .ToListAsync();

                        foreach (var oldIns in otherInsurances)
                        {
                            oldIns.IsCurrent = false;
                        }
                    }

                    _context.Update(insurance);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Insurances.Any(e => e.Id == insurance.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index), new { id = insurance.VehicleId });
            }

            var vehicleList = await _context.Vehicles
                                    .Select(v => new {
                                        v.VehicleId,
                                        DisplayText = v.LicensePlate ?? (v.Make + " " + v.Model)
                                    })
                                    .ToListAsync();

            ViewBag.VehicleList = new SelectList(vehicleList, "VehicleId", "DisplayText", insurance.VehicleId);

            return View(insurance);
        }

        // GET: Insurance/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var insurance = await _context.Insurances
                .Include(i => i.Vehicle)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (insurance == null)
            {
                return NotFound();
            }

            return View(insurance);
        }

        // POST: Insurance/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var insurance = await _context.Insurances.FindAsync(id);
            if (insurance != null)
            {
                _context.Insurances.Remove(insurance);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index), new { id = insurance.VehicleId });
            }


            return RedirectToAction("Index", "Vehicles");
        }

    }
}
