using Fleet_Managment_Production.Data;
using Fleet_Managment_Production.Models;
using Fleet_Managment_Production.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fleet_Managment_Production.Controllers
{
    [Authorize (Roles = "Admin,Manager")]
    public class ReportsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public ReportsController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Reports 
        public IActionResult Index()
        {
            var model = new ReportsViewModel
            {
                StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                EndDate = DateTime.Now,
                SelectedReport = ReportType.TCO
            };
            return View(model);
        }

        // POST: /Reports 
        [HttpPost]
        public async Task<IActionResult> Index(ReportsViewModel model)
        {
            if (!model.StartDate.HasValue) model.StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            if (!model.EndDate.HasValue) model.EndDate = DateTime.Now;

            switch (model.SelectedReport)
            {
                case ReportType.TCO:
                    await GenerateTcoReport(model);
                    break;
                case ReportType.Fuel:
                    await GenerateFuelReport(model);
                    break;
                case ReportType.Service:
                    await GenerateServiceReport(model);
                    break;
                case ReportType.DriverActivity:
                    await GenerateDriverReport(model);
                    break;
                case ReportType.Alerts:
                    await GenerateAlertsReport(model);
                    break;
            }

            return View(model);
        }

        // --- 1. RAPORT TCO ---
        private async Task GenerateTcoReport(ReportsViewModel model)
        {
            var vehicles = await _context.Vehicles
                .Include(v => v.Costs)
                .Include(v => v.Services)
                .ToListAsync();

            model.TcoData = vehicles.Select(v => new TcoReportItem
            {
                VehicleName = $"{v.Make} {v.Model}",
                LicensePlate = v.LicensePlate,

                // Poprawiono: uzycie 'Kwota' zamiast 'Amount' oraz usunięto '?? 0'
                FuelCost = v.Costs.Where(c => c.Type == CostType.Paliwo && c.Data >= model.StartDate && c.Data <= model.EndDate).Sum(c => c.Amount),
                // Poprawiono: usunięto '?? 0' z s.Cost
                ServiceCost = v.Services.Where(s => s.EntryDate >= model.StartDate && s.EntryDate <= model.EndDate).Sum(s => s.Cost)
            })
            .Where(x => x.TotalCost > 0)
            .OrderByDescending(x => x.TotalCost)
            .ToList();
        }

        // --- 2. RAPORT SPALANIA ---
        private async Task GenerateFuelReport(ReportsViewModel model)
        {
            // 1. Zabezpieczenie zakresu dat (ustawiamy koniec na 23:59:59 danego dnia)
            var start = model.StartDate?.Date ?? DateTime.MinValue;
            var end = model.EndDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.MaxValue;

            var vehicles = await _context.Vehicles.Include(v => v.Costs).ToListAsync();
            var fuelReportList = new List<FuelReportItem>();

            foreach (var v in vehicles)
            {
                // 2. Pobieramy wszystkie koszty paliwa dla pojazdu w wybranym przedziale
                var fuelCosts = v.Costs
                    .Where(c => c.Type == CostType.Paliwo && c.Data >= start && c.Data <= end)
                    .OrderBy(c => c.CurrentOdometer)
                    .ToList();

                // Jeśli pojazd nie ma ani jednego paragonu na paliwo -> pomijamy go w tabeli
                if (!fuelCosts.Any()) continue;

                int distance = 0;

                // 3. Sumujemy tylko te litry, które faktycznie zapisały się w bazie (nie są null)
                double liters = fuelCosts.Where(c => c.Liters.HasValue).Sum(c => c.Liters.Value);

                // 4. Obliczamy dystans (wymaga min. 2 wpisów z podanym przebiegiem)
                var costsWithOdo = fuelCosts.Where(c => c.CurrentOdometer.HasValue).ToList();
                if (costsWithOdo.Count >= 2)
                {
                    distance = costsWithOdo.Last().CurrentOdometer.Value - costsWithOdo.First().CurrentOdometer.Value;
                }

                // Dodajemy auto do raportu NIEZALEŻNIE od tego, czy dało się policzyć dystans
                fuelReportList.Add(new FuelReportItem
                {
                    VehicleName = $"{v.Make} {v.Model}",
                    LicensePlate = v.LicensePlate,
                    DistanceTraveled = distance,
                    TotalLiters = liters
                });
            }

            // Sortujemy i przekazujemy do widoku
            model.FuelData = fuelReportList.OrderByDescending(x => x.TotalLiters).ToList();
        }

        // --- 3. RAPORT SERWISÓW ---
        private async Task GenerateServiceReport(ReportsViewModel model)
        {
            var vehicles = await _context.Vehicles.Include(v => v.Services).ToListAsync();

            model.ServiceData = vehicles.Select(v => new ServiceReportItem
            {
                VehicleName = $"{v.Make} {v.Model}",
                LicensePlate = v.LicensePlate,
                ServiceCount = v.Services.Count(s => s.EntryDate >= model.StartDate && s.EntryDate <= model.EndDate),

                // Poprawiono: usunięto '?? 0' z s.Cost
                TotalServiceCost = v.Services.Where(s => s.EntryDate >= model.StartDate && s.EntryDate <= model.EndDate).Sum(s => s.Cost)
            })
            .Where(x => x.ServiceCount > 0)
            .OrderByDescending(x => x.TotalServiceCost)
            .ToList();
        }

        // --- 4. RAPORT KIEROWCÓW ---
        private async Task GenerateDriverReport(ReportsViewModel model)
        {
            var drivers = await _context.Drivers.Include(d => d.Trips).ToListAsync();

            model.DriverActivityData = drivers.Select(d => new DriverActivityReportItem
            {
                DriverName = $"{d.FirstName} {d.LastName}",
                TripsCount = d.Trips.Count(t => t.StartDate >= model.StartDate && t.StartDate <= model.EndDate),

                // Poprawiono: użycie 'RealDistance' z Twojego modelu Trip
                TotalDistance = d.Trips.Where(t => t.StartDate >= model.StartDate && t.StartDate <= model.EndDate).Sum(t => t.RealDistance)
            })
            .Where(x => x.TripsCount > 0)
            .OrderByDescending(x => x.TotalDistance)
            .ToList();
        }

        // --- 5. RAPORT ALERTÓW ---
        private async Task GenerateAlertsReport(ReportsViewModel model)
        {
            var vehicles = await _context.Vehicles
                .Include(v => v.Insurances)
                .Include(v => v.Inspections)
                .ToListAsync();

            var alerts = new List<AlertsReportItem>();
            var today = DateTime.Today;
            var warningThreshold = today.AddDays(30);

            foreach (var v in vehicles)
            {
                // Ubezpieczenia - zakładam, że mają właściwość ExpiryDate
                var endingInsurance = v.Insurances.FirstOrDefault(i => i.ExpiryDate <= warningThreshold && i.ExpiryDate >= today);
                if (endingInsurance != null)
                {
                    alerts.Add(new AlertsReportItem { VehicleName = $"{v.Make} {v.Model}", LicensePlate = v.LicensePlate, AlertType = "Koniec Ubezpieczenia", ExpiryDate = endingInsurance.ExpiryDate });
                }

                // Poprawiono: W modelu Inspection to 'NextInspectionDate'
                var endingInspection = v.Inspections.FirstOrDefault(i => i.NextInspectionDate.HasValue && i.NextInspectionDate.Value <= warningThreshold && i.NextInspectionDate.Value >= today);
                if (endingInspection != null)
                {
                    alerts.Add(new AlertsReportItem
                    {
                        VehicleName = $"{v.Make} {v.Model}",
                        LicensePlate = v.LicensePlate,
                        AlertType = "Koniec Przeglądu",
                        ExpiryDate = endingInspection.NextInspectionDate.Value // Pobieramy wartość
                    });
                }
            }

            model.AlertsData = alerts.OrderBy(a => a.ExpiryDate).ToList();
        }
    }
}