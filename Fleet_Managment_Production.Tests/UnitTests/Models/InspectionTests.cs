using Xunit;
using Fleet_Managment_Production.Models;
using Fleet_Managment_Production.Tests.Helpers;
using System;
using System.Linq;

namespace Fleet_Managment_Production.Tests.UnitTests.Models
{
    public class InspectionTests
    {
        // ==========================================
        // FABRYKA 
        // ==========================================
        private Inspection CreateValidInspection()
        {
            return new Inspection
            {
                VehicleId = 1,
                InspectionDate = DateTime.Today,
                Cost = 150.00m,
                Mileage = 120000,
                Description = "Standardowy przegląd techniczny",
                IsResultPositive = true,
                NextInspectionDate = DateTime.Today.AddYears(1),
                IsActive = true
            };
        }

        // ==========================================
        // TESTY DANYCH LICZBOWYCH I GRANICZNYCH
        // ==========================================

        [Theory]
        [InlineData(-1)]
        [InlineData(-1000)]
        public void Mileage_NegativeValue_FailsValidation(int invalidMileage)
        {
            var inspection = CreateValidInspection();
            inspection.Mileage = invalidMileage;

            var errors = ValidationHelper.ValidateModel(inspection);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Inspection.Mileage)));
        }

        [Theory]
        [InlineData(-0.01)]
        [InlineData(-150.00)]
        [InlineData(1000001)] // Powyżej dopuszczalnego miliona
        public void Cost_OutsideAllowedRange_FailsValidation(decimal invalidCost)
        {
            var inspection = CreateValidInspection();
            inspection.Cost = invalidCost;

            var errors = ValidationHelper.ValidateModel(inspection);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Inspection.Cost)));
        }

        [Fact]
        public void Cost_AtZero_PassesValidation()
        {
            // Przegląd darmowy (np. w ramach pakietu dealerskiego) powinien być dopuszczalny
            var inspection = CreateValidInspection();
            inspection.Cost = 0.00m;

            var errors = ValidationHelper.ValidateModel(inspection);

            Assert.DoesNotContain(errors, e => e.MemberNames.Contains(nameof(Inspection.Cost)));
        }

        // ==========================================
        // TESTY OCHRONY BAZY DANYCH (Over-posting)
        // ==========================================

        [Fact]
        public void Description_ExceedingMaxLength_FailsValidation()
        {
            var inspection = CreateValidInspection();
            inspection.Description = new string('A', 501); // 501 znaków (limit 500)

            var errors = ValidationHelper.ValidateModel(inspection);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Inspection.Description)));
        }

        // ==========================================
        // TESTY LOGIKI BIZNESOWEJ (Chronologia)
        // ==========================================

        [Fact]
        public void Validate_NextInspectionDateBeforeInspectionDate_ReturnsValidationError()
        {
            var inspection = CreateValidInspection();
            inspection.InspectionDate = DateTime.Today;
            // BŁĄD: Użytkownik ustawia następny przegląd w przeszłości!
            inspection.NextInspectionDate = DateTime.Today.AddDays(-1);

            var errors = ValidationHelper.ValidateModel(inspection);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Inspection.NextInspectionDate)));
        }

        [Fact]
        public void Validate_NextInspectionDateSameAsInspectionDate_ReturnsValidationError()
        {
            var inspection = CreateValidInspection();
            inspection.InspectionDate = DateTime.Today;
            // BŁĄD: Następny przegląd nie może być dokładnie tego samego dnia
            inspection.NextInspectionDate = DateTime.Today;

            var errors = ValidationHelper.ValidateModel(inspection);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Inspection.NextInspectionDate)));
        }

        [Fact]
        public void OptionalFields_WhenNull_PassesValidation()
        {
            var inspection = CreateValidInspection();

            // Te pola nie są wymuszone w modelu [Required]
            inspection.Description = null;
            inspection.Mileage = null;
            inspection.IsResultPositive = null;
            inspection.NextInspectionDate = null;

            var errors = ValidationHelper.ValidateModel(inspection);

            Assert.Empty(errors);
        }

        // ==========================================
        // HAPPY PATH
        // ==========================================

        [Fact]
        public void Inspection_FullyValidModel_PassesAllValidation()
        {
            var inspection = CreateValidInspection();

            var errors = ValidationHelper.ValidateModel(inspection);

            Assert.Empty(errors);
        }
        [Fact]
        public void VehicleId_NotSelected_FailsValidation()
        {
            var inspection = CreateValidInspection();
            inspection.VehicleId = 0;

            var errors = ValidationHelper.ValidateModel(inspection);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Inspection.VehicleId)));
        }
    }
}