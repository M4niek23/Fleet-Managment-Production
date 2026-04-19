using Fleet_Managment_Production.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Fleet_Managment_Production.Tests.IntegrationTests
{
    public class DriverIntegrationTests : IntegrationTestBase
    {
        // ==========================================
        // TEST 1: Twarda blokada duplikatów PESEL
        // ==========================================
        [Fact]
        public async Task AddDriver_WithDuplicatePesel_ShouldThrowDbUpdateException()
        {
            // Arrange
            var driver1 = new Driver { FirstName = "Jan", LastName = "Kowalski", Pesel = "90010112345", Status = DriverStatus.Active };
            _context.Drivers.Add(driver1);
            await _context.SaveChangesAsync();

            // Act
            var driver2 = new Driver { FirstName = "Anna", LastName = "Nowak", Pesel = "90010112345", Status = DriverStatus.Active };
            _context.Drivers.Add(driver2);

            // Assert
            await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
        }

        // ==========================================
        // TEST 2: Poprawne ładowanie relacji (Trasy)
        // ==========================================
        [Fact]
        public async Task SaveDriver_WithTrips_ShouldRetrieveDriverWithTripsIncluded()
        {
            // Arrange
            var driver = new Driver { FirstName = "Piotr", LastName = "Z", Pesel = "85020212345", Status = DriverStatus.Active };
            var vehicle = new Vehicle { Make = "Ford", Model = "Transit", VIN = "VIN123", LicensePlate = "W1", ProductionYear = 2021 };

            _context.Drivers.Add(driver);
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync(); 

            var trip = new Trip
            {
                DriverId = driver.Id,
                VehicleId = vehicle.VehicleId,
                StartLocation = "Warszawa",
                EndLocation = "Kraków",
                StartOdometer = 10000,
                StartDate = DateTime.Now
            };
            _context.Trips.Add(trip);
            await _context.SaveChangesAsync();

            // Act
            var driverFromDb = await _context.Drivers
                .Include(d => d.Trips)
                .FirstOrDefaultAsync(d => d.Id == driver.Id);

            // Assert
            Assert.NotNull(driverFromDb);
            Assert.Single(driverFromDb.Trips);
            Assert.Equal("Warszawa", driverFromDb.Trips.First().StartLocation);
        }

        // ==========================================
        // TEST 3: Weryfikacja atrybutu [NotMapped]
        // ==========================================
        [Fact]
        public async Task SaveDriver_IgnoresNotMappedProperties()
        {
            // Arrange
            var driver = new Driver
            {
                FirstName = "Michał",
                LastName = "W",
                Pesel = "95030312345",
                SelectedCategories = new System.Collections.Generic.List<LicenseCategory> { LicenseCategory.B, LicenseCategory.C }
            };

            // Act
            _context.Drivers.Add(driver);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Assert
            var savedDriver = await _context.Drivers.FindAsync(driver.Id);
            Assert.NotNull(savedDriver);

            Assert.Empty(savedDriver.SelectedCategories);
        }
    }
}