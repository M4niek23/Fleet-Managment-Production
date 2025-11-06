using System.Diagnostics;
using Fleet_Managment_Production.Models;
using Microsoft.AspNetCore.Mvc;

// --- DODAJ TE LINIE ---
using Fleet_Managment_Production.Data;        // Dostêp do AppDbContext
using Fleet_Managment_Production.Models.Home; // Dostêp do DashboardViewModel
using Microsoft.EntityFrameworkCore;        // Dla .Include() i .ToListAsync()
using System.Linq;                          // Dla .Where() i .OrderBy()
using System.Threading.Tasks;               // Dla async/await
using System;                               // Dla DateTime
// ----------------------

namespace Fleet_Managment_Production.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context; // <-- DODANE: Pole dla bazy danych

        // Zaktualizowany konstruktor, aby przyjmowa³ AppDbContext
        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context; // <-- DODANE
        }

        // ZAST¥PIONA ca³a metoda Index()
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            // Ustawiamy horyzont czasowy alertów (np. 30 dni)
            var alertLimitDate = today.AddDays(30);

            // 1. Pobierz koñcz¹ce siê ubezpieczenia
            var expiringInsurances = await _context.Insurances
                .Include(i => i.Vehicle) // Wa¿ne: Do³¹czamy dane pojazdu, by znaæ rejestracjê
                .Where(i => i.ExpiryDate >= today && i.ExpiryDate <= alertLimitDate)
                .OrderBy(i => i.ExpiryDate) // Poka¿ najwczeœniej wygasaj¹ce na górze
                .ToListAsync();

            // 2. Stwórz ViewModel i przeka¿ go do widoku
            var viewModel = new DashboardViewModel
            {
                ExpiringInsurances = expiringInsurances
            };

            return View(viewModel); // Przeka¿ model do widoku
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