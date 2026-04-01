using Fleet_Managment_Production.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

// Alias 'M' zapewnia, że "Service" z Modelu nie pomyli się kompilatorowi z folderem "Services"
using M = Fleet_Managment_Production.Models;

namespace Fleet_Managment_Production.Services
{
    public class SeedService
    {
        public static async Task SeedDatabase(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<M.Users>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<SeedService>>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            try
            {
                logger.LogInformation("Sprawdzanie, czy baza danych istnieje.");
                await context.Database.MigrateAsync();

                logger.LogInformation("Rozpoczynanie inicjowania ról.");
                await AddRoleAsync(roleManager, "Admin");
                await AddRoleAsync(roleManager, "User");
                await AddRoleAsync(roleManager, "Manager");
                await AddRoleAsync(roleManager, "Moderator");

                logger.LogInformation("Dodawanie administratora.");
                var adminEmail = config["AdminSettings:Email"] ?? "admin@fleet.com";
                var adminPassword = config["AdminSettings:Password"] ?? "Admin@123";
                if (await userManager.FindByEmailAsync(adminEmail) == null)
                {
                    var adminUser = new M.Users
                    {
                        FullName = "Admin FleetManager",
                        UserName = adminEmail,
                        NormalizedUserName = adminEmail.ToUpper(),
                        Email = adminEmail,
                        NormalizedEmail = adminEmail.ToUpper(),
                        EmailConfirmed = true,
                        SecurityStamp = Guid.NewGuid().ToString()
                    };

                    var result = await userManager.CreateAsync(adminUser, adminPassword);
                    if (result.Succeeded)
                    {
                        logger.LogInformation("Przypisanie roli administratora do użytkownika admin.");
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                    }
                }

                // URUCHOMIENIE GENERATORA DANYCH TESTOWYCH
                await SeedDummyDataAsync(context, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Wystąpił błąd podczas indeksowania bazy danych.");
            }
        }

        private static async Task AddRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        private static async Task SeedDummyDataAsync(AppDbContext context, ILogger logger)
        {
            if (!await context.Vehicles.AnyAsync(v => v.LicensePlate == "TEST001"))
            {
                logger.LogInformation("Generowanie danych testowych do raportów...");

                var now = DateTime.Now;
                var baseDate = new DateTime(now.Year, now.Month, 15);
                var today = DateTime.Today;

                // 1. POJAZDY
                var v1 = new M.Vehicle { Make = "Skoda", Model = "Octavia", LicensePlate = "TEST001" };
                var v2 = new M.Vehicle { Make = "Toyota", Model = "Corolla", LicensePlate = "TEST002" };
                context.Vehicles.AddRange(v1, v2);
                await context.SaveChangesAsync();

                // 2. KIEROWCY (U kierowcy zostaje Id, bo w modelu Driver masz 'public int Id')
                var d1 = new M.Driver { FirstName = "Jan", LastName = "Kowalski", Pesel = "90010112345", PhoneNumber = "123456789" };
                var d2 = new M.Driver { FirstName = "Anna", LastName = "Nowak", Pesel = "92020254321", PhoneNumber = "987654321" };
                context.Drivers.AddRange(d1, d2);
                await context.SaveChangesAsync();

                // 3. KOSZTY (Zamieniono v1.Id na v1.VehicleId)
                context.Costs.AddRange(
                    new M.Cost { VehicleId = v1.VehicleId, Type = M.CostType.Paliwo, Opis = "Paliwo", Data = baseDate.AddDays(-10), Kwota = 300m, CurrentOdometer = 100000, Liters = 0 },
                    new M.Cost { VehicleId = v1.VehicleId, Type = M.CostType.Paliwo, Opis = "Paliwo", Data = baseDate.AddDays(-2), Kwota = 250m, CurrentOdometer = 100600, Liters = 42 },
                    new M.Cost { VehicleId = v2.VehicleId, Type = M.CostType.Paliwo, Opis = "Paliwo", Data = baseDate.AddDays(-8), Kwota = 200m, CurrentOdometer = 50000, Liters = 0 },
                    new M.Cost { VehicleId = v2.VehicleId, Type = M.CostType.Paliwo, Opis = "Paliwo", Data = baseDate.AddDays(-1), Kwota = 280m, CurrentOdometer = 50500, Liters = 45 }
                );

                // 4. SERWISY 
                context.Services.AddRange(
                    new M.Service { VehicleId = v1.VehicleId, Cost = 1200m, EntryDate = baseDate.AddDays(-12), Description = "Wymiana rozrządu" },
                    new M.Service { VehicleId = v2.VehicleId, Cost = 350m, EntryDate = baseDate.AddDays(-7), Description = "Klocki hamulcowe" }
                );

                // 5. TRASY
                context.Trips.AddRange(
                    new M.Trip
                    {
                        VehicleId = v1.VehicleId,
                        DriverId = d1.Id,
                        StartLocation = "Warszawa",
                        EndLocation = "Kraków",
                        StartDate = baseDate.AddDays(-8),
                        EndTime = baseDate.AddDays(-7),
                        StartOdometer = 100000,
                        EndOdometer = 100300,
                        TripType = M.TripType.Business
                    },
                    new M.Trip
                    {
                        VehicleId = v1.VehicleId,
                        DriverId = d2.Id,
                        StartLocation = "Kraków",
                        EndLocation = "Katowice",
                        StartDate = baseDate.AddDays(-6),
                        EndTime = baseDate.AddDays(-6),
                        StartOdometer = 100300,
                        EndOdometer = 100380,
                        TripType = M.TripType.Business
                    }
                );

                // 6. ALERTY 
                context.Insurances.AddRange(
                    new M.Insurance { VehicleId = v1.VehicleId, PolicyNumber = "POL-111222", InsurareName = "PZU", StartDate = today.AddDays(-350), ExpiryDate = today.AddDays(14), Cost = 1500m }
                );

                context.Inspections.AddRange(
                    new M.Inspection { VehicleId = v2.VehicleId, InspectionDate = today.AddDays(-345), NextInspectionDate = today.AddDays(20), Cost = 150m, Description = "Przegląd" }
                );

                await context.SaveChangesAsync();
                logger.LogInformation("Pomyślnie wygenerowano flotę testową do raportów!");
            }
        }
    }
}