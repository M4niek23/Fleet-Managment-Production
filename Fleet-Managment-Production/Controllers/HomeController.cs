using System.Diagnostics;
using Fleet_Managment_Production.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Fleet_Managment_Production.Data;       
using Microsoft.EntityFrameworkCore;        
using Fleet_Managment_Production.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Fleet_Managment_Production.Controllers
{
    [Authorize]
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

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            bool isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");

            var insurancesQuery = _context.Insurances.Include(i => i.Vehicle).AsQueryable();
            var inspectionsQuery = _context.Inspections.Include(i => i.Vehicle).AsQueryable();

            if (!isAdminOrManager)
            {
                insurancesQuery = insurancesQuery.Where(i => i.Vehicle.Driver.UserId == userId);
                inspectionsQuery = inspectionsQuery.Where(i => i.Vehicle.Driver.UserId == userId);
            }

            var expiringInsurances = await insurancesQuery
                .Where(i => i.ExpiryDate >= today && i.ExpiryDate <= alertLimitDate)
                .OrderBy(i => i.ExpiryDate)
                .ToListAsync();

            var expiringInspections = await inspectionsQuery
                .Where(i => i.NextInspectionDate >= today && i.NextInspectionDate <= alertLimitDate)
                .OrderBy(i => i.NextInspectionDate)
                .ToListAsync();

            var viewModel = new DashboardViewModel
            {
                ExpiringInsurances = expiringInsurances,
                ExpiringInspections = expiringInspections
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