using Fleet_Managment_Production.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Fleet_Managment_Production.Tests.IntegrationTests
{
    public class CostIntegrationTests : IntegrationTestBase
    {
        // ==========================================
        // TEST 1: Persystencja i Navigation Properties
        // ==========================================
        [Fact]
        public async Task SaveCost_ShouldBeRetrievableWithVehicleData()
        {
            var vehicle = new Vehicle
            {
                Make = "Skoda",
                Model = "Octavia",
                VIN = "T3STV1N0000000000",
                LicensePlate = "KR12345",
                ProductionYear = 2022
            };
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            var cost = new Cost
            {
                VehicleId = vehicle.VehicleId,
                Type = CostType.Paliwo,
                Amount = 350.75m,
                Data = DateTime.Now,
                Liters = 55.5
            };

            // Act
            _context.Costs.Add(cost);
            await _context.SaveChangesAsync();

            // Assert
            var costFromDb = await _context.Costs
                .Include(c => c.Vehicle)
                .FirstOrDefaultAsync(c => c.Id == cost.Id);

            Assert.NotNull(costFromDb);
            Assert.Equal(350.75m, costFromDb.Amount);
            Assert.Equal("Skoda", costFromDb.Vehicle?.Make); 
        }

        // ==========================================
        // TEST 2: Integralność danych (FK Constraint)
        // ==========================================
        [Fact]
        public async Task AddCost_WithNonExistentVehicleId_ShouldThrowException()
        {
            // Arrange
            var cost = new Cost
            {
                VehicleId = 999,
                Amount = 100m,
                Type = CostType.Inne,
                Data = DateTime.Now
            };

            // Act & Assert
            _context.Costs.Add(cost);

            await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
        }

        // ==========================================
        // TEST 3: Precyzja finansowa (Decimal Check)
        // ==========================================
        [Fact]
        public async Task CostAmount_ShouldMaintainPrecisionAfterStorage()
        {
            // Arrange
            var vehicle = new Vehicle { Make = "X", Model = "Y", VIN = "V1", LicensePlate = "L1", ProductionYear = 2020 };
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            decimal preciseAmount = 123.4567m;

            var cost = new Cost { VehicleId = vehicle.VehicleId, Amount = preciseAmount, Type = CostType.Serwis, Data = DateTime.Now };

            // Act
            _context.Costs.Add(cost);
            await _context.SaveChangesAsync();

            // Assert
            var savedCost = await _context.Costs.FindAsync(cost.Id);
            Assert.Equal(Math.Round(preciseAmount, 2), savedCost.Amount);
        }
        // ==========================================
        // TEST 4: Raportowanie i Agregacja Danych
        // ==========================================
        [Fact]
        public async Task GetTotalFuelCost_ForSpecificVehicle_CalculatesCorrectly()
        {
            // Arrange
            var vehicle = new Vehicle { Make = "Fiat", Model = "Panda", VIN = "VIN999", LicensePlate = "WA12345", ProductionYear = 2020 };
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            _context.Costs.AddRange(
                new Cost { VehicleId = vehicle.VehicleId, Type = CostType.Paliwo, Amount = 100.50m, Liters = 20 },
                new Cost { VehicleId = vehicle.VehicleId, Type = CostType.Paliwo, Amount = 50.00m, Liters = 10 },
                new Cost { VehicleId = vehicle.VehicleId, Type = CostType.Serwis, Amount = 2000.00m }
            );
            await _context.SaveChangesAsync();

            // Act
            var fuelCosts = await _context.Costs
                  .Where(c => c.VehicleId == vehicle.VehicleId && c.Type == CostType.Paliwo)
                  .Select(c => c.Amount)
                  .ToListAsync();

            var totalFuelCost = fuelCosts.Sum();

            // Assert
            Assert.Equal(150.50m, totalFuelCost);
        }
    }
}