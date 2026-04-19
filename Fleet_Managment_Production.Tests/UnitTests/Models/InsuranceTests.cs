using Xunit;
using Fleet_Managment_Production.Models;
using Fleet_Managment_Production.Tests.Helpers;
using System;
using System.Linq;

namespace Fleet_Managment_Production.Tests.UnitTests.Models
{
    public class InsuranceTests
    {
        // ==========================================
        // FABRYKA
        // ==========================================
        private Insurance CreateValidInsurance()
        {
            return new Insurance
            {
                PolicyNumber = "POLISA123",
                InsurareName = "PZU",
                StartDate = DateTime.Today,
                ExpiryDate = DateTime.Today.AddYears(1),
                Cost = 1500.50m,
                HasOc = true,
                AcScope = AcScope.Full,
                VehicleId = 1,
                IsCurrent = true
            };
        }

        // ==========================================
        // TESTY FORMATU (Regex i Długości)
        // ==========================================

        [Theory]
        [InlineData("POL-123")]
        [InlineData("POL 123")]  
        [InlineData("POL/123")]   
        [InlineData("POL!@#")]    
        public void PolicyNumber_WithSpecialCharacters_FailsValidation(string invalidPolicyNumber)
        {
            var insurance = CreateValidInsurance();
            insurance.PolicyNumber = invalidPolicyNumber;

            var errors = ValidationHelper.ValidateModel(insurance);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Insurance.PolicyNumber)));
        }

        [Fact]
        public void InsurareName_ExceedingMaxLength_FailsValidation()
        {
            var insurance = CreateValidInsurance();
            insurance.InsurareName = new string('X', 101);

            var errors = ValidationHelper.ValidateModel(insurance);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Insurance.InsurareName)));
        }

        // ==========================================
        // TESTY BIZNESOWE (Chronologia Dat)
        // ==========================================

        [Fact]
        public void Validate_ExpiryDateBeforeStartDate_ReturnsValidationError()
        {
            var insurance = CreateValidInsurance();
            insurance.StartDate = DateTime.Today;
           
            insurance.ExpiryDate = DateTime.Today.AddDays(-1);

            var errors = ValidationHelper.ValidateModel(insurance);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Insurance.ExpiryDate)));
        }

        [Fact]
        public void Validate_ExpiryDateSameAsStartDate_ReturnsValidationError()
        {
            var insurance = CreateValidInsurance();
            insurance.StartDate = DateTime.Today;
            insurance.ExpiryDate = DateTime.Today;

            var errors = ValidationHelper.ValidateModel(insurance);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Insurance.ExpiryDate)));
        }

        // ==========================================
        // TESTY DANYCH LICZBOWYCH I KLUCZY
        // ==========================================

        [Theory]
        [InlineData(-0.01)]
        [InlineData(-500.00)]
        public void Cost_NegativeAmount_FailsValidation(decimal invalidCost)
        {
            var insurance = CreateValidInsurance();
            insurance.Cost = invalidCost;

            var errors = ValidationHelper.ValidateModel(insurance);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Insurance.Cost)));
        }

        [Fact]
        public void AcScope_WithInvalidEnumValue_FailsValidation()
        {
            var insurance = CreateValidInsurance();
            insurance.AcScope = (AcScope)99;

            var errors = ValidationHelper.ValidateModel(insurance);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Insurance.AcScope)));
        }

        [Fact]
        public void VehicleId_WhenNull_FailsValidation()
        {
            var insurance = CreateValidInsurance();
            insurance.VehicleId = null;

            var errors = ValidationHelper.ValidateModel(insurance);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Insurance.VehicleId)));
        }

        // ==========================================
        // HAPPY PATH
        // ==========================================

        [Fact]
        public void Insurance_FullyValidModel_PassesAllValidation()
        {
            var insurance = CreateValidInsurance();

            var errors = ValidationHelper.ValidateModel(insurance);

            Assert.Empty(errors);
        }
      
        [Fact]
        public void Validate_ExpiredInsuranceMarkedAsCurrent_ReturnsValidationError()
        {
            // Arrange
            var insurance = CreateValidInsurance();
            insurance.StartDate = DateTime.Today.AddYears(-2);
            insurance.ExpiryDate = DateTime.Today.AddDays(-1);
            insurance.IsCurrent = true;

            // Act
            var errors = ValidationHelper.ValidateModel(insurance);

            // Assert
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Insurance.IsCurrent)));
        }
    }
}