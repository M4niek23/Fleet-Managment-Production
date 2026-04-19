using Fleet_Managment_Production.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Fleet_Managment_Production.Tests.IntegrationTests
{
    public class ServiceIntegrationTests : IntegrationTestBase
    {
        // ==========================================
        // TEST 1: Klucz Obcy (Foreign Key Integrity)
        // ==========================================
        [Fact]
        public async Task AddService_WithInvalidVehicleId_ShouldThrowException()
        {
            // Arrange
            var service = new Service
            {
                Description = "Wymiana klocków hamulcowych",
                EntryDate = DateTime.Now,
                Cost = 450.00m,
                VehicleId = 99999 
            };

            // Act & Assert
            _context.Services.Add(service);
            await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
        }

        // ==========================================
        // TEST 2: Pobieranie z relacją i Właściwość Wyliczana
        // ==========================================
        [Fact]
        public async Task SaveService_ShouldBeRetrievableWithVehicleDetails()
        {
            // Arrange
            var vehicle = new Vehicle { Make = "Skoda", Model = "Octavia", VIN = "VIN_SERV_1", LicensePlate = "WA1010", ProductionYear = 2022 };
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            var service = new Service
            {
                Description = "Naprawa silnika",
                EntryDate = DateTime.Now.AddDays(-2),
                ActualEndDate = DateTime.Now, 
                Cost = 1250.75m,
                VehicleId = vehicle.VehicleId
            };
            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            _context.ChangeTracker.Clear();

            // Act
            var savedService = await _context.Services
                .Include(s => s.Vehicle)
                .FirstOrDefaultAsync(s => s.Id == service.Id);

            // Assert
            Assert.NotNull(savedService);
            Assert.NotNull(savedService.Vehicle);
            Assert.Equal("Skoda", savedService.Vehicle.Make);
            Assert.Equal(1250.75m, savedService.Cost);

            Assert.True(savedService.IsFinished);
        }

        // ==========================================
        // TEST 3: Usuwanie Kaskadowe (Cascade Delete)
        // ==========================================
        [Fact]
        public async Task DeleteVehicle_ShouldRemoveAllItsServiceHistory()
        {
            // Arrange
            var vehicle = new Vehicle { Make = "Ford", Model = "Focus", VIN = "VIN_SERV_2", LicensePlate = "WB2020", ProductionYear = 2019 };
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            _context.Services.AddRange(
                new Service { Description = "Wymiana opon na zimowe", Cost = 1000, VehicleId = vehicle.VehicleId, EntryDate = DateTime.Now },
                new Service { Description = "Serwis klimatyzacji", Cost = 300, VehicleId = vehicle.VehicleId, EntryDate = DateTime.Now }
            );
            await _context.SaveChangesAsync();

            _context.ChangeTracker.Clear();

            // Act
            var vehicleFromDb = await _context.Vehicles.FindAsync(vehicle.VehicleId);
            _context.Vehicles.Remove(vehicleFromDb);
            await _context.SaveChangesAsync();

            // Assert
            var servicesCount = await _context.Services.CountAsync(s => s.VehicleId == vehicle.VehicleId);
            Assert.Equal(0, servicesCount);
        }
    }
}