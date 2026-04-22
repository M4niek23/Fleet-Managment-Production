using Xunit;
using Fleet_Managment_Production.Models;
using Fleet_Managment_Production.Tests.Helpers;
using System;
using System.Linq;

namespace Fleet_Managment_Production.Tests.UnitTests.Models
{
    public class CostTests
    {
        // ==========================================
        // FABRYKA (Zapewnia poprawny obiekt bazowy)
        // ==========================================
        private Cost CreateValidCost()
        {
            return new Cost
            {
                VehicleId = 1,
                Type = CostType.Paliwo,
                Amount = 250.50m,
                Data = DateTime.Now,
                Description = "Tankowanie PB95",
                Liters = 45.5,
                CurrentOdometer = 125000,
                IsFullTank = true
            };
        }

        // ==========================================
        // TESTY WARTOŚCI DOMYŚLNYCH
        // ==========================================

        [Fact]
        public void Constructor_SetsDefaultIsFullTank_ToFalse()
        {
            // Arrange & Act
            var cost = new Cost();

            // Assert
            Assert.False(cost.IsFullTank);
        }

        // ==========================================
        // TESTY WALIDACJI: Kwota (Amount)
        // ==========================================

        [Theory]
        [InlineData(0.00)]    
        [InlineData(-10.50)]  
        [InlineData(1000001)] 
        public void Amount_OutsideRange_FailsValidation(decimal invalidAmount)
        {
            var cost = CreateValidCost();
            cost.Amount = invalidAmount;

            var errors = ValidationHelper.ValidateModel(cost);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Cost.Amount)));
        }

        [Fact]
        public void Amount_AtMinimumValue_PassesValidation()
        {
            var cost = CreateValidCost();
            cost.Amount = 0.01m;

            var errors = ValidationHelper.ValidateModel(cost);

            Assert.DoesNotContain(errors, e => e.MemberNames.Contains(nameof(Cost.Amount)));
        }

        // ==========================================
        // TESTY WALIDACJI: Pojazd (VehicleId)
        // ==========================================

        [Fact]
        public void VehicleId_Required_Missing_FailsValidation()
        {
            var cost = new Cost { Amount = 100m, Type = CostType.Inne };

            var errors = ValidationHelper.ValidateModel(cost);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Cost.VehicleId)));
        }

        // ==========================================
        // TESTY WALIDACJI: Typ Kosztu (Enum)
        // ==========================================

        [Fact]
        public void Type_WithInvalidEnumValue_FailsValidation()
        {
            var cost = CreateValidCost();
            cost.Type = (CostType)99;

            var errors = ValidationHelper.ValidateModel(cost);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Cost.Type)));
        }

        // ==========================================
        // TESTY WALIDACJI: Dane opcjonalne (Paliwo)
        // ==========================================

        [Fact]
        public void OptionalFields_WhenNull_PassesValidation()
        {
            // Arrange
            var cost = CreateValidCost();

            cost.Type = CostType.Serwis;
            cost.IsFullTank = false;

            // Act
            cost.Description = null;
            cost.Liters = null;
            cost.CurrentOdometer = null;

            var errors = ValidationHelper.ValidateModel(cost);

            // Assert
            Assert.Empty(errors);
        }
        // ==========================================
        // TESTY LOGIKI: Przebieg (Odometer)
        // ==========================================

        [Theory]
        [InlineData(-1)]
        [InlineData(-500)]
        public void CurrentOdometer_NegativeValue_ShouldBeHandled(int invalidOdometer)
        {
            var cost = CreateValidCost();
            cost.CurrentOdometer = invalidOdometer;

            var errors = ValidationHelper.ValidateModel(cost);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Cost.CurrentOdometer)));
        }

        // ==========================================
        // HAPPY PATH
        // ==========================================

        [Fact]
        public void Cost_FullyValidModel_PassesAllValidation()
        {
            var cost = CreateValidCost();

            var errors = ValidationHelper.ValidateModel(cost);

            Assert.Empty(errors);
        }
        [Fact]
        public void Validate_FuelCost_WithoutLiters_ReturnsValidationError()
        {
            // Arrange
            var cost = CreateValidCost();
            cost.Type = CostType.Paliwo;
            cost.Liters = null;

            // Act
            var errors = ValidationHelper.ValidateModel(cost);

            // Assert
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Cost.Liters)));
        }

        [Fact]
        public void Validate_NonFuelCost_WithLiters_ReturnsValidationError()
        {
            // Arrange
            var cost = CreateValidCost();
            cost.Type = CostType.Ubezpieczenie;
            cost.Liters = 50.5; 

            // Act
            var errors = ValidationHelper.ValidateModel(cost);

            // Assert
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Cost.Liters)));
        }

        // ==========================================
        // TESTY OCHRONY BAZY DANYCH
        // ==========================================

        [Fact]
        public void Description_ExceedingMaxLength_FailsValidation()
        {
            var cost = CreateValidCost();
            cost.Description = new string('A', 501); 

            var errors = ValidationHelper.ValidateModel(cost);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Cost.Description)));
        }

        [Fact]
        public void Data_FutureDate_FailsValidation()
        {
            var cost = CreateValidCost();
            cost.Data = DateTime.Now.AddDays(5);

            var errors = ValidationHelper.ValidateModel(cost);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Cost.Data)));
        }
        [Fact]
        public void Validate_NonFuelCost_WithIsFullTankTrue_ReturnsValidationError()
        {
            // Arrange
            var cost = CreateValidCost();
            cost.Type = CostType.Serwis; 
            cost.Liters = null;          
            cost.IsFullTank = true;      

            // Act
            var errors = ValidationHelper.ValidateModel(cost);

            // Assert
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Cost.IsFullTank)));
        }
    }
}