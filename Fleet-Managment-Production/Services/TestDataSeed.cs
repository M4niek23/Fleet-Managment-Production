using Fleet_Managment_Production.Data;
using Fleet_Managment_Production.Models;
using Bogus;
namespace Fleet_Managment_Production.Services
{
    public class TestDataSeed
    {
        public static async Task SeedTestDataAsync(AppDbContext context)
        {
            if (context.Vehicles.Any()) return;

            Randomizer.Seed = new Random(12345);
            var locale = "pl";

            var driverFaker = new Faker<Driver>(locale)
                .RuleFor(d => d.FirstName, f => f.Name.FirstName())
                .RuleFor(d => d.LastName, f => f.Name.LastName())
                .RuleFor(d => d.Pesel, f => f.Random.Replace("###########"))
                .RuleFor(d => d.LicenseCategory, f => f.PickRandom<LicenseCategory>())
                .RuleFor(d => d.PhoneNumber, f => f.Phone.PhoneNumber())
                .RuleFor(d => d.Email, f => f.Internet.Email());

            var drivers = driverFaker.Generate(50);
            await context.Drivers.AddRangeAsync(drivers);
            await context.SaveChangesAsync();

            var vehicleFaker = new Faker<Vehicle>(locale)
                .RuleFor(v => v.Make, f => f.Vehicle.Manufacturer())
                .RuleFor(v => v.Model, f => f.Vehicle.Model())
                .RuleFor(v => v.FuelType, f => f.PickRandom<FuelType>())
                .RuleFor(v => v.ProductionYear, f => f.Random.Int(2010, 2024))
                .RuleFor(v => v.LicensePlate, f => f.Random.Replace("W? #####").ToUpper())
                .RuleFor(v => v.VIN, f => f.Vehicle.Vin())
                .RuleFor(v => v.CurrentKm, f => f.Random.Int(1000, 250000))
                .RuleFor(v => v.Status, f => f.PickRandom<VehicleStatus>())
                .RuleFor(v => v.DriverId, f => f.PickRandom(drivers).Id);

            var vehicles = vehicleFaker.Generate(50);
            await context.Vehicles.AddRangeAsync(vehicles);
            await context.SaveChangesAsync();

            var insuranceFaker = new Faker<Insurance>(locale)
                .RuleFor(i => i.PolicyNumber, f => f.Random.AlphaNumeric(10).ToUpper())
                .RuleFor(i => i.InsurareName, f => f.Company.CompanyName())
                .RuleFor(i => i.StartDate, f => f.Date.Past(1))
                .RuleFor(i => i.ExpiryDate, (f, i) => i.StartDate.AddYears(1))
                .RuleFor(i => i.Cost, f => f.Random.Decimal(500, 5000))
                .RuleFor(i => i.HasOc, true)
                .RuleFor(i => i.AcScope, f => f.PickRandom<AcScope>())
                .RuleFor(i => i.HasAssistance, f => f.Random.Bool())
                .RuleFor(i => i.HasNNW, f => f.Random.Bool())
                .RuleFor(i => i.IsCurrent, f => f.Random.Bool())
                .RuleFor(i => i.VehicleId, f => f.PickRandom(vehicles).VehicleId);

            var insurances = insuranceFaker.Generate(50);
            await context.Insurances.AddRangeAsync(insurances);

            var inspectionFaker = new Faker<Inspection>(locale)
                .RuleFor(i => i.InspectionDate, f => f.Date.Past(2))
                .RuleFor(i => i.Description, f => f.Lorem.Sentence())
                .RuleFor(i => i.Mileage, f => f.Random.Int(1000, 250000))
                .RuleFor(i => i.Cost, f => f.Random.Decimal(100, 400))
                .RuleFor(i => i.VehicleId, f => f.PickRandom(vehicles).VehicleId)
                .RuleFor(i => i.IsResultPositive, f => f.Random.Bool(0.9f))
                .RuleFor(i => i.NextInspectionDate, (f, i) => i.InspectionDate.AddYears(1))
                .RuleFor(i => i.IsActive, true);

            var inspections = inspectionFaker.Generate(50);
            await context.Inspections.AddRangeAsync(inspections);

            var serviceFaker = new Faker<Service>(locale)
                .RuleFor(s => s.VehicleId, f => f.PickRandom(vehicles).VehicleId)
                .RuleFor(s => s.Description, f => f.Lorem.Sentence(3))
                .RuleFor(s => s.Cost, f => f.Random.Decimal(200, 3000))
                .RuleFor(s => s.EntryDate, f => f.Date.Past(1))
                .RuleFor(s => s.PlannedEndDate, (f, s) => s.EntryDate.AddDays(f.Random.Int(1, 10)))
                .RuleFor(s => s.ActualEndDate, (f, s) => s.EntryDate.AddDays(f.Random.Int(1, 12)));

            var services = serviceFaker.Generate(50);
            await context.Services.AddRangeAsync(services);

            var tripFaker = new Faker<Trip>(locale)
                .RuleFor(t => t.VehicleId, f => f.PickRandom(vehicles).VehicleId)
                .RuleFor(t => t.DriverId, f => f.PickRandom(drivers).Id)
                .RuleFor(t => t.StartDate, f => f.Date.Past(1))
                .RuleFor(t => t.EndTime, (f, t) => t.StartDate.AddHours(f.Random.Int(1, 24)))
                .RuleFor(t => t.StartLocation, f => f.Address.City())
                .RuleFor(t => t.EndLocation, f => f.Address.City())
                .RuleFor(t => t.StartOdometer, f => f.Random.Int(1000, 200000))
                .RuleFor(t => t.EndOdometer, (f, t) => t.StartOdometer + f.Random.Int(10, 1000))
                .RuleFor(t => t.Description, f => f.Lorem.Word())
                .RuleFor(t => t.TripType, f => f.PickRandom<TripType>());

            var trips = tripFaker.Generate(50);
            await context.Trips.AddRangeAsync(trips);

            var costFaker = new Faker<Cost>(locale)
                .RuleFor(c => c.VehicleId, f => f.PickRandom(vehicles).VehicleId)
                .RuleFor(c => c.Type, f => f.PickRandom(CostType.Paliwo, CostType.Inne))
                .RuleFor(c => c.Opis, f => f.Lorem.Sentence())
                .RuleFor(c => c.Kwota, f => f.Random.Decimal(50, 500))
                .RuleFor(c => c.Data, f => f.Date.Past(1))
                .RuleFor(c => c.Liters, (f, c) => c.Type == CostType.Paliwo ? (double?)f.Random.Double(10, 60) : null)
                .RuleFor(c => c.IsFullTank, (f, c) => c.Type == CostType.Paliwo ? f.Random.Bool() : false);

            var costs = costFaker.Generate(50);
            await context.Costs.AddRangeAsync(costs);

            await context.SaveChangesAsync();
        }
    }
}


