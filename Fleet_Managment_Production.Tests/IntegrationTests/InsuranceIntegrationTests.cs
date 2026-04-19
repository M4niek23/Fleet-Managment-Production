using Fleet_Managment_Production.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Fleet_Managment_Production.Tests.IntegrationTests
{
    public class InsuranceIntegrationTests : IntegrationTestBase
    {
        // ==========================================
        // TEST 1: Weryfikacja integralności klucza obcego
        // ==========================================
        [Fact]
        public async Task AddInsurance_WithNonExistentVehicleId_ShouldThrowDbUpdateException()
        {
            // Arrange 
            var insurance = new Insurance
            {
                PolicyNumber = "POL12345",
                InsurareName = "PZU",
                StartDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddYears(1),
                Cost = 1500.00m,
                VehicleId = 9999 
            };

            // Act & Assert
            _context.Insurances.Add(insurance);
            await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
        }

        // ==========================================
        // TEST 2: Poprawne ładowanie (Eager Loading)
        // ==========================================
        [Fact]
        public async Task SaveInsurance_ShouldRetrieveWithAssociatedVehicleData()
        {
            // Arrange
            var vehicle = new Vehicle { Make = "Volvo", Model = "XC60", VIN = "VIN_INS_1", LicensePlate = "GD12345", ProductionYear = 2021 };
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            var insurance = new Insurance
            {
                PolicyNumber = "POL98765",
                InsurareName = "Warta",
                StartDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddYears(1),
                Cost = 2100.99m,
                AcScope = AcScope.Full,
                VehicleId = vehicle.VehicleId
            };
            _context.Insurances.Add(insurance);
            await _context.SaveChangesAsync();

            _context.ChangeTracker.Clear();

            // Act
            var savedInsurance = await _context.Insurances
                .Include(i => i.Vehicle)
                .FirstOrDefaultAsync(i => i.Id == insurance.Id);

            // Assert
            Assert.NotNull(savedInsurance);
            Assert.NotNull(savedInsurance.Vehicle);
            Assert.Equal("Volvo", savedInsurance.Vehicle.Make);
            Assert.Equal(2100.99m, savedInsurance.Cost);
        }

        // ==========================================
        // TEST 3: Usuwanie Kaskadowe (Cascade Delete)
        // ==========================================
        [Fact]
        public async Task DeleteVehicle_ShouldCascadeDeleteAssociatedInsurance()
        {
            // Arrange
            var vehicle = new Vehicle { Make = "Toyota", Model = "Corolla", VIN = "VIN_INS_2", LicensePlate = "WZ12345", ProductionYear = 2018 };
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            var insurance = new Insurance
            {
                PolicyNumber = "POL55555",
                InsurareName = "Allianz",
                StartDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddYears(1),
                Cost = 1200.50m,
                VehicleId = vehicle.VehicleId
            };
            _context.Insurances.Add(insurance);
            await _context.SaveChangesAsync();

            _context.ChangeTracker.Clear();

            // Act
            var vehicleToDelete = await _context.Vehicles.FindAsync(vehicle.VehicleId);
            _context.Vehicles.Remove(vehicleToDelete);
            await _context.SaveChangesAsync();

            // Assert
            var insuranceInDb = await _context.Insurances.FirstOrDefaultAsync(i => i.VehicleId == vehicle.VehicleId);

            Assert.Null(insuranceInDb);
        }
    }
}