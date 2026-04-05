using Fleet_Managment_Production.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
    }
}