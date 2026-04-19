using Fleet_Managment_Production.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Fleet_Managment_Production.Tests.IntegrationTests
{
    public class TripIntegrationTests : IntegrationTestBase
    {
        // ==========================================
        // TEST 1: Ochrona Podwójnego Klucza Obcego
        // ==========================================
        [Fact]
        public async Task AddTrip_WithNonExistentDriverOrVehicle_ShouldThrowException()
        {
            // Arrange
            var trip = new Trip
            {
                VehicleId = 999, // BŁĄD
                DriverId = 888,  // BŁĄD
                StartLocation = "Warszawa",
                EndLocation = "Kraków",
                StartOdometer = 10000,
                TripType = TripType.Business
            };

            // Act & Assert
            _context.Trips.Add(trip);
            await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
        }

        // ==========================================
        // TEST 2: Ładowanie relacji (Include) i [NotMapped]
        // ==========================================
        [Fact]
        public async Task SaveTrip_ShouldLoadRelationships_AndCalculateRealDistance()
        {
            // Arrange
            var driver = new Driver { FirstName = "Robert", LastName = "Kubica", Pesel = "84120712345", Status = DriverStatus.Active };
            var vehicle = new Vehicle { Make = "BMW", Model = "M5", VIN = "VIN_TRIP_1", LicensePlate = "KR999", ProductionYear = 2023 };

            _context.Drivers.Add(driver);
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            var trip = new Trip
            {
                VehicleId = vehicle.VehicleId,
                DriverId = driver.Id,
                StartLocation = "Kraków",
                EndLocation = "Poznań",
                StartOdometer = 50000,
                EndOdometer = 50450,
                TripType = TripType.Business
            };
            _context.Trips.Add(trip);
            await _context.SaveChangesAsync();

            _context.ChangeTracker.Clear();

            // Act
            var savedTrip = await _context.Trips
                .Include(t => t.Vehicle)
                .Include(t => t.Driver)
                .FirstOrDefaultAsync(t => t.Id == trip.Id);

            // Assert
            Assert.NotNull(savedTrip);
            Assert.NotNull(savedTrip.Vehicle);
            Assert.NotNull(savedTrip.Driver);

            Assert.Equal("BMW", savedTrip.Vehicle.Make);
            Assert.Equal("Robert", savedTrip.Driver.FirstName);

            Assert.Equal(450, savedTrip.RealDistance);
        }

        // ==========================================
        // TEST 3: Ochrona przed usunięciem historii (DeleteBehavior.Restrict)
        // ==========================================
        [Fact]
        public async Task DeleteDriver_WithAssociatedTrips_ShouldBeBlockedByDatabase()
        {
            // Arrange
            var driver = new Driver { FirstName = "Janusz", LastName = "Tracz", Pesel = "50010112345", Status = DriverStatus.Active };
            var vehicle = new Vehicle { Make = "Fiat", Model = "Ducato", VIN = "VIN_TRIP_2", LicensePlate = "LU999", ProductionYear = 2015 };

            _context.Drivers.Add(driver);
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            _context.Trips.Add(new Trip
            {
                VehicleId = vehicle.VehicleId,
                DriverId = driver.Id,
                StartLocation = "Lublin",
                EndLocation = "Chełm",
                StartOdometer = 100
            });
            await _context.SaveChangesAsync();

            _context.ChangeTracker.Clear();

            // Act & Assert
            var driverToDelete = await _context.Drivers.FindAsync(driver.Id);
            _context.Drivers.Remove(driverToDelete);

            await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
        }
    }
}