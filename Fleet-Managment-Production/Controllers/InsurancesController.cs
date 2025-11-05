// Fleet-Managment-Production/Controllers/InsuranceController.cs

using Fleet_Managment_Production.Data;
using Fleet_Managment_Production.Models; // Dla Insurance i AcScope
using Fleet_Managment_Production.Models.VehicleTable; // Dla Twojego modelu Vehicle!
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

[Authorize]
public class InsuranceController : Controller
{
    private readonly AppDbContext _context;

    public InsuranceController(AppDbContext context)
    {
        _context = context;
    }

    // GET: Insurance/Index/5 (id to VehicleId)
    public async Task<IActionResult> Index(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        // Znajdź pojazd po jego kluczu głównym (VehicleId) i załaduj jego ubezpieczenia
        var vehicle = await _context.Vehicles
            .Include(v => v.Insurances)
            .FirstOrDefaultAsync(m => m.VehicleId == id); // Używamy VehicleId

        if (vehicle == null)
        {
            return NotFound();
        }

        // Przekażemy ID pojazdu i jego numer rejestracyjny do widoku
        ViewBag.VehicleId = id;
        ViewBag.VehicleRegistration = vehicle.LicensePlate ?? "Brak Rejestracji"; // Używamy LicensePlate

        // Sortujemy ubezpieczenia, aby najnowsze (lub aktywne) były na górze
        var insurances = vehicle.Insurances.OrderByDescending(i => i.IsCurrent).ThenByDescending(i => i.ExpiryDate);

        return View(insurances.ToList());
    }

    // GET: Insurance/Create?vehicleId=5
    public async Task<IActionResult> Create(int vehicleId)
    {
        var vehicle = await _context.Vehicles.FindAsync(vehicleId); // Znajdź pojazd po VehicleId
        if (vehicle == null)
        {
            return NotFound();
        }

        ViewBag.VehicleId = vehicleId;
        ViewBag.VehicleRegistration = vehicle.LicensePlate ?? "Brak Rejestracji";

        return View(new Insurance
        {
            VehicleId = vehicleId,
            StartDate = DateTime.Today,
            ExpiryDate = DateTime.Today.AddYears(1),
            IsCurrent = true // Sugeruj, że nowe ubezpieczenie jest "aktualne"
        });
    }

    // POST: Insurance/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("PolicyNumber,InsurerName,StartDate,ExpiryDate,Cost,VehicleId,HasOc,HasAssistance,AcScope,IsCurrent")]
        Insurance insurance)
    {
        if (ModelState.IsValid)
        {
            // WAŻNA LOGIKA: Jeśli to ubezpieczenie jest ustawione jako "Aktualne",
            // musimy odznaczyć wszystkie inne ubezpieczenia dla tego pojazdu.
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

        // Jeśli model nie jest poprawny, musimy ponownie załadować dane do ViewBaga
        var vehicle = await _context.Vehicles.FindAsync(insurance.VehicleId);
        ViewBag.VehicleId = insurance.VehicleId;
        ViewBag.VehicleRegistration = vehicle?.LicensePlate ?? "Brak Rejestracji";

        return View(insurance);
    }

    // (Później dodaj akcje Edit i Delete, które będą działać analogicznie)
}