using System.Diagnostics;
using Fleet_Managment_Production.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Fleet_Managment_Production.Data;       
using Microsoft.EntityFrameworkCore;        
using Fleet_Managment_Production.Models;

namespace Fleet_Managment_Production.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context; 
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var alertLimitDate = today.AddDays(30);

            // Istniej¹ca logika dla ubezpieczeñ
            var expiringInsurances = await _context.Insurances
                .Include(i => i.Vehicle)
                .Where(i => i.ExpiryDate >= today && i.ExpiryDate <= alertLimitDate)
                .OrderBy(i => i.ExpiryDate)
                .ToListAsync();

            // NOWA LOGIKA: Pobieranie koñcz¹cych siê przegl¹dów
            var expiringInspections = await _context.Inspections
                .Include(i => i.Vehicle)
                .Where(i => i.NextInspectionDate >= today && i.NextInspectionDate <= alertLimitDate)
                .OrderBy(i => i.NextInspectionDate)
                .ToListAsync();

            var viewModel = new DashboardViewModel
            {
                ExpiringInsurances = expiringInsurances,
                ExpiringInspections = expiringInspections // Przekazujemy do widoku
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}