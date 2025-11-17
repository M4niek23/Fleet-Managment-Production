using System.Diagnostics;
using Fleet_Managment_Production.Models;
using Microsoft.AspNetCore.Mvc;
using Fleet_Managment_Production.Data;       
using Microsoft.EntityFrameworkCore;        

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

            var expiringInsurances = await _context.Insurances
                .Include(i => i.Vehicle)
                .Where(i => i.ExpiryDate >= today && i.ExpiryDate <= alertLimitDate)
                .OrderBy(i => i.ExpiryDate)
                .ToListAsync();
           

            var viewModel = new DashboardViewModel
            {
                ExpiringInsurances = expiringInsurances
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