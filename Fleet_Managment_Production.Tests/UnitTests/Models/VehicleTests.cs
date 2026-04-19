using Xunit;
using Fleet_Managment_Production.Models;
using Fleet_Managment_Production.Tests.Helpers;
using System;
using System.Linq;

namespace Fleet_Managment_Production.Tests.UnitTests.Models
{
    public class VehicleTests
    {
        // ==========================================
        // FABRYKA
        // ==========================================
        private Vehicle CreateValidVehicle()
        {
            return new Vehicle
            {
                Make = "Toyota",
                Model = "Corolla",
                FuelType = FuelType.Hybryda,
                ProductionYear = DateTime.Now.Year,
                LicensePlate = "WAW12345",
                VIN = "1T1ABCD1234567890", // 17 poprawnych znaków (bez I, O, Q)
                CurrentKm = 15000,
                Status = VehicleStatus.Available
            };
        }

        // ==========================================
        // TESTY INICJALIZACJI KOLEKCJI
        // ==========================================

        [Fact]
        public void Constructor_InitializesCollections_ToPreventNullReferences()
        {
            var vehicle = new Vehicle();

            Assert.NotNull(vehicle.Inspections);
            Assert.NotNull(vehicle.Insurances);
            Assert.NotNull(vehicle.Costs);
            Assert.NotNull(vehicle.Trips);
            Assert.NotNull(vehicle.Services);

            Assert.Empty(vehicle.Inspections);
            Assert.Empty(vehicle.Insurances);
        }

        // ==========================================
        // TESTY VIN (RegularExpression i Length)
        // ==========================================

        [Theory]
        [InlineData("1234567890123456")]   // 16 znaków (za krótki)
        [InlineData("123456789012345678")] // 18 znaków (za długi)
        public void VIN_IncorrectLength_FailsValidation(string invalidVin)
        {
            var vehicle = CreateValidVehicle();
            vehicle.VIN = invalidVin;

            var errors = ValidationHelper.ValidateModel(vehicle);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Vehicle.VIN)));
        }

        [Theory]
        [InlineData("1T1ABCD123456789I")] // Zawiera niedozwoloną literę 'I'
        [InlineData("1T1ABCD123456789O")] // Zawiera niedozwoloną literę 'O'
        [InlineData("1T1ABCD123456789Q")] // Zawiera niedozwoloną literę 'Q'
        [InlineData("1T1abcd1234567890")] // Zawiera małe litery (Regex wymaga wielkich)
        [InlineData("1T1ABCD12345678!@")] // Znaki specjalne
        public void VIN_WithInvalidCharacters_FailsValidation(string invalidVin)
        {
            var vehicle = CreateValidVehicle();
            vehicle.VIN = invalidVin;

            var errors = ValidationHelper.ValidateModel(vehicle);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Vehicle.VIN)));
        }

        [Fact]
        public void VIN_CorrectFormat_PassesValidation()
        {
            var vehicle = CreateValidVehicle();
            vehicle.VIN = "WBA00000000000000"; // Przykładowy poprawny VIN BMW

            var errors = ValidationHelper.ValidateModel(vehicle);

            Assert.DoesNotContain(errors, e => e.MemberNames.Contains(nameof(Vehicle.VIN)));
        }

        // ==========================================
        // TESTY ROKU PRODUKCJI (Logika i Range)
        // ==========================================

        [Fact]
        public void ProductionYear_Before1886_FailsValidation()
        {
            var vehicle = CreateValidVehicle();
            vehicle.ProductionYear = 1885; // Przed wynalezieniem samochodu wg atrybutu Range

            var errors = ValidationHelper.ValidateModel(vehicle);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Vehicle.ProductionYear)));
        }

        [Fact]
        public void Validate_ProductionYearInFarFuture_ReturnsValidationError()
        {
            var vehicle = CreateValidVehicle();
            vehicle.ProductionYear = DateTime.Now.Year + 2; // Auto z dalekiej przyszłości

            var errors = ValidationHelper.ValidateModel(vehicle);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Vehicle.ProductionYear)));
        }

        [Fact]
        public void Validate_ProductionYearNextYear_PassesValidation()
        {
            var vehicle = CreateValidVehicle();
            vehicle.ProductionYear = DateTime.Now.Year + 1; // Modele na kolejny rok są dozwolone

            var errors = ValidationHelper.ValidateModel(vehicle);

            Assert.DoesNotContain(errors, e => e.MemberNames.Contains(nameof(Vehicle.ProductionYear)));
        }

        // ==========================================
        // TESTY OCHRONY BAZY I ENUMÓW
        // ==========================================

        [Fact]
        public void MakeAndModel_ExceedingMaxLength_FailsValidation()
        {
            var vehicle = CreateValidVehicle();
            vehicle.Make = new string('A', 51);
            vehicle.Model = new string('B', 51);

            var errors = ValidationHelper.ValidateModel(vehicle);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Vehicle.Make)));
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Vehicle.Model)));
        }

        [Fact]
        public void Enums_WithInvalidValues_FailsValidation()
        {
            var vehicle = CreateValidVehicle();
            vehicle.FuelType = (FuelType)99;
            vehicle.Status = (VehicleStatus)99;

            var errors = ValidationHelper.ValidateModel(vehicle);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Vehicle.FuelType)));
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Vehicle.Status)));
        }

        [Fact]
        public void CurrentKm_NegativeValue_FailsValidation()
        {
            var vehicle = CreateValidVehicle();
            vehicle.CurrentKm = -1;

            var errors = ValidationHelper.ValidateModel(vehicle);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Vehicle.CurrentKm)));
        }

        // ==========================================
        // HAPPY PATH
        // ==========================================

        [Fact]
        public void Vehicle_FullyValidModel_PassesAllValidation()
        {
            var vehicle = CreateValidVehicle();

            var errors = ValidationHelper.ValidateModel(vehicle);

            Assert.Empty(errors);
        }
    }
}