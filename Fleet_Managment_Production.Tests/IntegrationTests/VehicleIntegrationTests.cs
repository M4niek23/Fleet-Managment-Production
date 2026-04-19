using Fleet_Managment_Production.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Fleet_Managment_Production.Tests.IntegrationTests
{
    public class VehicleIntegrationTests : IntegrationTestBase
    {
        // ==========================================
        // TEST 1: Unikalność Numeru VIN
        // ==========================================
        [Fact]
        public async Task AddVehicle_WithDuplicateVin_ShouldThrowException()
        {
            // Arrange
            var v1 = new Vehicle
            {
                Make = "Ford",
                Model = "Focus",
                VIN = "ABC12345678901234",
                LicensePlate = "WA111",
                ProductionYear = 2020,
                CurrentKm = 50000
            };
            _context.Vehicles.Add(v1);
            await _context.SaveChangesAsync();

            // Act
            var v2 = new Vehicle
            {
                Make = "Opel",
                Model = "Astra",
                VIN = "ABC12345678901234",
                LicensePlate = "WA222",
                ProductionYear = 2021,
                CurrentKm = 1000
            };
            _context.Vehicles.Add(v2);

            // Assert
            await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
        }

        // ==========================================
        // TEST 2: Unikalność Numeru Rejestracyjnego
        // ==========================================
        [Fact]
        public async Task AddVehicle_WithDuplicateLicensePlate_ShouldThrowException()
        {
            // Arrange
            var v1 = new Vehicle
            {
                Make = "Audi",
                Model = "A3",
                VIN = "VIN11111111111111",
                LicensePlate = "PO77777",
                ProductionYear = 2019,
                CurrentKm = 100000
            };
            _context.Vehicles.Add(v1);
            await _context.SaveChangesAsync();

            // Act
            var v2 = new Vehicle
            {
                Make = "BMW",
                Model = "X3",
                VIN = "VIN22222222222222",
                LicensePlate = "PO77777",
                ProductionYear = 2020,
                CurrentKm = 5000
            };
            _context.Vehicles.Add(v2);

            // Assert
            await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
        }

        // ==========================================
        // TEST 3: Ładowanie Relacji (User i Driver)
        // ==========================================
        [Fact]
        public async Task SaveVehicle_WithUserAndDriver_ShouldRetrieveCorrectly()
        {
            // Arrange
            var driver = new Driver { FirstName = "Piotr", LastName = "K", Pesel = "12345678901", Status = DriverStatus.Active };

            var user = new Users
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "admin@fleet.pl",
                FullName = "Administrator Systemu"
            };

            _context.Drivers.Add(driver);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var vehicle = new Vehicle
            {
                Make = "Tesla",
                Model = "3",
                VIN = "TESLA123456789012",
                LicensePlate = "E1TEST",
                ProductionYear = 2023,
                CurrentKm = 100,
                DriverId = driver.Id,
                UserId = user.Id
            };
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            _context.ChangeTracker.Clear();

            // Act
            var savedVehicle = await _context.Vehicles
                .Include(v => v.Driver)
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.VehicleId == vehicle.VehicleId);

            // Assert
            Assert.NotNull(savedVehicle);
            Assert.Equal("Tesla", savedVehicle.Make);
            Assert.Equal("Administrator Systemu", savedVehicle.User.FullName);
        }

        // ==========================================
        // TEST 4: Sprawdzenie Cascade Delete (Koszty)
        // ==========================================
        [Fact]
        public async Task DeleteVehicle_ShouldCascadeDeleteCosts()
        {
            // Arrange
            var vehicle = new Vehicle
            {
                Make = "Toyota",
                Model = "RAV4",
                VIN = "TOYOTA12345678901",
                LicensePlate = "RAV123",
                ProductionYear = 2022,
                CurrentKm = 30000
            };
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            var cost = new Cost { VehicleId = vehicle.VehicleId, Type = CostType.Serwis, Amount = 500, Data = DateTime.Now };
            _context.Costs.Add(cost);
            await _context.SaveChangesAsync();

            _context.ChangeTracker.Clear();

            // Act
            var vFromDb = await _context.Vehicles.FindAsync(vehicle.VehicleId);
            _context.Vehicles.Remove(vFromDb);
            await _context.SaveChangesAsync();

            // Assert
            var costExists = await _context.Costs.AnyAsync(c => c.VehicleId == vehicle.VehicleId);
            Assert.False(costExists);
        }
    }
}