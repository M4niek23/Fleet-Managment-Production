using Fleet_Managment_Production.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Fleet_Managment_Production.Tests.IntegrationTests
{
    public class InspectionIntegrationTests : IntegrationTestBase
    {
        // ==========================================
        // TEST 1: Weryfikacja integralności klucza obcego
        // ==========================================
        [Fact]
        public async Task AddInspection_WithNonExistentVehicleId_ShouldThrowException()
        {
            // Arrange
            var inspection = new Inspection
            {
                VehicleId = 9999,
                InspectionDate = DateTime.Now,
                Cost = 150.00m
            };

            // Act & Assert
            _context.Inspections.Add(inspection);
            await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
        }

        // ==========================================
        // TEST 2: Poprawne ładowanie (Eager Loading)
        // ==========================================
        [Fact]
        public async Task SaveInspection_WithValidVehicle_ShouldLoadRelationships()
        {
            // Arrange
            var vehicle = new Vehicle { Make = "BMW", Model = "X5", VIN = "VIN_INSP_1", LicensePlate = "PO12345", ProductionYear = 2020 };
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            var inspection = new Inspection
            {
                VehicleId = vehicle.VehicleId,
                InspectionDate = DateTime.Now.AddDays(-10),
                NextInspectionDate = DateTime.Now.AddYears(1),
                Cost = 299.99m,
                IsResultPositive = true,
                IsActive = true
            };
            _context.Inspections.Add(inspection);
            await _context.SaveChangesAsync();

            _context.ChangeTracker.Clear();

            // Act
            var savedInsp = await _context.Inspections
                .Include(i => i.Vehicle)
                .FirstOrDefaultAsync(i => i.Id == inspection.Id);

            // Assert
            Assert.NotNull(savedInsp);
            Assert.NotNull(savedInsp.Vehicle);
            Assert.Equal("BMW", savedInsp.Vehicle.Make);
            Assert.Equal(299.99m, savedInsp.Cost);
        }

        // ==========================================
        // TEST 3: Usuwanie Kaskadowe (Cascade Delete)
        // ==========================================
        [Fact]
        public async Task DeleteVehicle_ShouldCascadeDeleteAssociatedInspections()
        {
            // Arrange
            var vehicle = new Vehicle { Make = "Mazda", Model = "6", VIN = "VIN_INSP_2", LicensePlate = "LU12345", ProductionYear = 2019 };
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            var inspection = new Inspection
            {
                VehicleId = vehicle.VehicleId,
                InspectionDate = DateTime.Now,
                Cost = 100m
            };
            _context.Inspections.Add(inspection);
            await _context.SaveChangesAsync();

            _context.ChangeTracker.Clear();

            // Act
            var vehicleToDelete = await _context.Vehicles.FindAsync(vehicle.VehicleId);
            _context.Vehicles.Remove(vehicleToDelete);
            await _context.SaveChangesAsync();

            // Assert
            var inspectionInDb = await _context.Inspections.FirstOrDefaultAsync(i => i.VehicleId == vehicle.VehicleId);

            Assert.Null(inspectionInDb);
        }
    }
}