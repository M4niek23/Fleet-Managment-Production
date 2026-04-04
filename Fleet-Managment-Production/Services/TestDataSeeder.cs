using Bogus;
using Fleet_Managment_Production.Data;
using Fleet_Managment_Production.Models;
using Microsoft.AspNetCore.Identity;

namespace Fleet_Managment_Production.Services
{
    public class TestDataSeeder
    {
        public static async Task SeedTestDataAsync(AppDbContext context, UserManager<Users> userManager)
        {
            if (context.Vehicles.Count() >= 50) return;

            Randomizer.Seed = new Random(12345);
            var sysRand = new Random(12345); // Standardowy generator C# dla pętli
            var locale = "pl";

            // 1. GENEROWANIE 50 KONT UŻYTKOWNIKÓW IDENTITY
            var usersList = new List<Users>();
            var userFaker = new Faker("pl");

            for (int i = 0; i < 50; i++)
            {
                var email = userFaker.Internet.Email();
                var user = new Users
                {
                    FullName = userFaker.Name.FullName(),
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, "KontoTestowe123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "User");
                    usersList.Add(user);
                }
            }

            // 2. GENEROWANIE KIEROWCÓW (Zaktualizowane dla nowego modelu LicenseCategories)
            var driverFaker = new Faker<Driver>(locale)
                .RuleFor(d => d.FirstName, (f, d) => f.Name.FirstName())
                .RuleFor(d => d.LastName, (f, d) => f.Name.LastName())
                .RuleFor(d => d.Pesel, (f, d) => f.Random.Replace("###########"))
                // Losujemy jedną kategorię prawy jazdy i zamieniamy na tekst (string) zgodnie z nowym modelem:
                .RuleFor(d => d.LicenseCategories, (f, d) => f.PickRandom<LicenseCategory>().ToString())
                .RuleFor(d => d.PhoneNumber, (f, d) => f.Phone.PhoneNumber());

            var drivers = driverFaker.Generate(50);

            // Bezpieczne przypisanie relacji do kont w zwykłej pętli
            for (int i = 0; i < drivers.Count; i++)
            {
                if (i < usersList.Count)
                {
                    drivers[i].UserId = usersList[i].Id;
                    drivers[i].Email = usersList[i].Email;
                }
            }

            await context.Drivers.AddRangeAsync(drivers);
            await context.SaveChangesAsync();

            // 3. GENEROWANIE POJAZDÓW
            var vehicleFaker = new Faker<Fleet_Managment_Production.Models.Vehicle>(locale)
                .RuleFor(v => v.Make, (f, v) => f.Vehicle.Manufacturer())
                .RuleFor(v => v.Model, (f, v) => f.Vehicle.Model())
                .RuleFor(v => v.FuelType, (f, v) => f.PickRandom<FuelType>())
                .RuleFor(v => v.ProductionYear, (f, v) => f.Random.Int(2010, 2024))
                .RuleFor(v => v.LicensePlate, (f, v) => f.Random.Replace("W? #####").ToUpper())
                .RuleFor(v => v.VIN, (f, v) => f.Vehicle.Vin())
                .RuleFor(v => v.CurrentKm, (f, v) => f.Random.Int(1000, 250000))
                .RuleFor(v => v.Status, (f, v) => f.PickRandom<VehicleStatus>());

            var vehicles = vehicleFaker.Generate(50);

            // Ręczne przypisanie kluczy obcych
            foreach (var v in vehicles)
            {
                v.DriverId = drivers[sysRand.Next(drivers.Count)].Id;
                v.UserId = usersList[sysRand.Next(usersList.Count)].Id;
            }

            await context.Vehicles.AddRangeAsync(vehicles);
            await context.SaveChangesAsync();

            // 4. GENEROWANIE UBEZPIECZEŃ
            var insuranceFaker = new Faker<Insurance>(locale)
                .RuleFor(i => i.PolicyNumber, (f, i) => f.Random.AlphaNumeric(10).ToUpper())
                .RuleFor(i => i.InsurareName, (f, i) => f.Company.CompanyName())
                .RuleFor(i => i.StartDate, (f, i) => f.Date.Past(1))
                .RuleFor(i => i.Cost, (f, i) => f.Random.Decimal(500, 5000))
                .RuleFor(i => i.HasOc, (f, i) => true)
                .RuleFor(i => i.AcScope, (f, i) => f.PickRandom<AcScope>())
                .RuleFor(i => i.HasAssistance, (f, i) => f.Random.Bool())
                .RuleFor(i => i.HasNNW, (f, i) => f.Random.Bool())
                .RuleFor(i => i.IsCurrent, (f, i) => f.Random.Bool());

            var insurances = insuranceFaker.Generate(50);

            foreach (var ins in insurances)
            {
                ins.VehicleId = vehicles[sysRand.Next(vehicles.Count)].VehicleId;
                ins.ExpiryDate = ins.StartDate.AddYears(1);
            }
            await context.Insurances.AddRangeAsync(insurances);

            // 5. GENEROWANIE PRZEGLĄDÓW
            var inspectionFaker = new Faker<Inspection>(locale)
                .RuleFor(i => i.InspectionDate, (f, i) => f.Date.Past(2))
                .RuleFor(i => i.Description, (f, i) => f.Lorem.Sentence())
                .RuleFor(i => i.Cost, (f, i) => f.Random.Decimal(100, 400))
                .RuleFor(i => i.IsActive, (f, i) => true);

            var inspections = inspectionFaker.Generate(50);

            foreach (var insp in inspections)
            {
                insp.VehicleId = vehicles[sysRand.Next(vehicles.Count)].VehicleId;
                insp.Mileage = sysRand.Next(1000, 250000);
                insp.IsResultPositive = sysRand.Next(100) > 10; // 90% szans
                insp.NextInspectionDate = insp.InspectionDate.AddYears(1);
            }
            await context.Inspections.AddRangeAsync(inspections);

            // 6. GENEROWANIE SERWISÓW
            var serviceFaker = new Faker<Service>(locale)
                .RuleFor(s => s.Description, (f, s) => f.Lorem.Sentence(3))
                .RuleFor(s => s.Cost, (f, s) => f.Random.Decimal(200, 3000))
                .RuleFor(s => s.EntryDate, (f, s) => f.Date.Past(1));

            var services = serviceFaker.Generate(50);

            foreach (var s in services)
            {
                s.VehicleId = vehicles[sysRand.Next(vehicles.Count)].VehicleId;
                s.PlannedEndDate = s.EntryDate.AddDays(sysRand.Next(1, 10));
                s.ActualEndDate = s.EntryDate.AddDays(sysRand.Next(1, 12));
            }
            await context.Services.AddRangeAsync(services);

            // 7. GENEROWANIE PODRÓŻY
            var tripFaker = new Faker<Trip>(locale)
                .RuleFor(t => t.StartDate, (f, t) => f.Date.Past(1))
                .RuleFor(t => t.StartLocation, (f, t) => f.Address.City())
                .RuleFor(t => t.EndLocation, (f, t) => f.Address.City())
                .RuleFor(t => t.StartOdometer, (f, t) => f.Random.Int(1000, 200000))
                .RuleFor(t => t.Description, (f, t) => f.Lorem.Word())
                .RuleFor(t => t.TripType, (f, t) => f.PickRandom<TripType>());

            var trips = tripFaker.Generate(50);

            foreach (var t in trips)
            {
                t.VehicleId = vehicles[sysRand.Next(vehicles.Count)].VehicleId;
                t.DriverId = drivers[sysRand.Next(drivers.Count)].Id;
                t.EndTime = t.StartDate.AddHours(sysRand.Next(1, 24));
                t.EndOdometer = t.StartOdometer + sysRand.Next(10, 1000);
            }
            await context.Trips.AddRangeAsync(trips);

            // 8. GENEROWANIE KOSZTÓW
            var costFaker = new Faker<Cost>(locale)
                .RuleFor(c => c.Type, (f, c) => f.PickRandom(CostType.Paliwo, CostType.Inne))
                .RuleFor(c => c.Opis, (f, c) => f.Lorem.Sentence())
                .RuleFor(c => c.Kwota, (f, c) => f.Random.Decimal(50, 500))
                .RuleFor(c => c.Data, (f, c) => f.Date.Past(1));

            var costs = costFaker.Generate(50);

            foreach (var c in costs)
            {
                c.VehicleId = vehicles[sysRand.Next(vehicles.Count)].VehicleId;
                if (c.Type == CostType.Paliwo)
                {
                    c.Liters = sysRand.NextDouble() * 50 + 10;
                    c.IsFullTank = sysRand.Next(2) == 0;
                }
            }
            await context.Costs.AddRangeAsync(costs);

            // Zapis wszystkiego na sam koniec
            await context.SaveChangesAsync();
        }
    }
}