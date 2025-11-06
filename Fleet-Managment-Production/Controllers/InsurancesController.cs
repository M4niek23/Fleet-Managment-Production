using Fleet_Managment_Production.Data;
using Fleet_Managment_Production.Models;
using Fleet_Managment_Production.Models.VehicleTable;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System; // 👈 POPRAWKA 1: Dodano brakujący using dla 'DateTime'
using System.Linq;
using System.Threading.Tasks;

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

            if (!_context.Vehicles.Any())
            {
                return View("NoVehicles");
            }


            if (id == null)
            {

                var firstVehicle = await _context.Vehicles.FirstOrDefaultAsync();
                if (firstVehicle == null)
                    return View("NoVehicles");

                return RedirectToAction(nameof(Index), new { id = firstVehicle.VehicleId });
            }

            var vehicle = await _context.Vehicles
                .Include(v => v.Insurances)
                .FirstOrDefaultAsync(m => m.VehicleId == id);

            if (vehicle == null)
            {
                return View("NoVehicles");
            }

            ViewBag.VehicleId = id;
            ViewBag.VehicleRegistration = vehicle.LicensePlate ?? "Brak Rejestracji";

            var insurances = vehicle.Insurances
                .OrderByDescending(i => i.IsCurrent)
                .ThenByDescending(i => i.ExpiryDate);

            return View(insurances.ToList());
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
