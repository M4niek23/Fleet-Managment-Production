using Xunit;
using Fleet_Managment_Production.Models;
using Fleet_Managment_Production.Tests.Helpers;
using System;
using System.Linq;

namespace Fleet_Managment_Production.Tests.UnitTests.Models
{
    public class ServiceTests
    {
        // ==========================================
        // FABRYKA
        // ==========================================
        private Service CreateValidService()
        {
            return new Service
            {
                VehicleId = 1,
                EntryDate = DateTime.Today,
                PlannedEndDate = DateTime.Today.AddDays(3),
                ActualEndDate = null,
                Description = "Wymiana rozrządu",
                Cost = 1500.00m
            };
        }

        // ==========================================
        // TESTY WŁAŚCIWOŚCI OBLICZANYCH (IsFinished)
        // ==========================================

        [Fact]
        public void IsFinished_ReturnsTrue_WhenActualEndDateHasValue()
        {
            var service = CreateValidService();
            service.ActualEndDate = DateTime.Today.AddDays(2);

            Assert.True(service.IsFinished);
        }

        [Fact]
        public void IsFinished_ReturnsFalse_WhenActualEndDateIsNull()
        {
            var service = CreateValidService();
            service.ActualEndDate = null;

            Assert.False(service.IsFinished);
        }

        // ==========================================
        // TESTY LOGIKI BIZNESOWEJ (Chronologia dat)
        // ==========================================

        [Fact]
        public void Validate_PlannedEndDateBeforeEntryDate_ReturnsValidationError()
        {
            var service = CreateValidService();
            service.EntryDate = DateTime.Today;
            service.PlannedEndDate = DateTime.Today.AddDays(-1);

            var errors = ValidationHelper.ValidateModel(service);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Service.PlannedEndDate)));
        }

        [Fact]
        public void Validate_ActualEndDateBeforeEntryDate_ReturnsValidationError()
        {
            var service = CreateValidService();
            service.EntryDate = DateTime.Today;
            service.ActualEndDate = DateTime.Today.AddDays(-5);

            var errors = ValidationHelper.ValidateModel(service);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Service.ActualEndDate)));
        }

        // ==========================================
        // TESTY BEZPIECZEŃSTWA BAZY DANYCH
        // ==========================================

        [Fact]
        public void Description_ExceedingMaxLength_FailsValidation()
        {
            var service = CreateValidService();
            service.Description = new string('A', 1001);

            var errors = ValidationHelper.ValidateModel(service);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Service.Description)));
        }

        [Fact]
        public void Description_WhenNullOrEmpty_FailsValidation()
        {
            var service = CreateValidService();
            service.Description = "";

            var errors = ValidationHelper.ValidateModel(service);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Service.Description)));
        }

        [Theory]
        [InlineData(-0.01)]
        [InlineData(-500.00)]
        [InlineData(1000001.00)]
        public void Cost_OutsideAllowedRange_FailsValidation(decimal invalidCost)
        {
            var service = CreateValidService();
            service.Cost = invalidCost;

            var errors = ValidationHelper.ValidateModel(service);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Service.Cost)));
        }

        [Fact]
        public void VehicleId_NotAssigned_FailsValidation()
        {
            var service = CreateValidService();
            service.VehicleId = 0;

            var errors = ValidationHelper.ValidateModel(service);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Service.VehicleId)));
        }

        // ==========================================
        // HAPPY PATH
        // ==========================================

        [Fact]
        public void Service_FullyValidModel_PassesAllValidation()
        {
            var service = CreateValidService();

            var errors = ValidationHelper.ValidateModel(service);

            Assert.Empty(errors);
        }
    }
}