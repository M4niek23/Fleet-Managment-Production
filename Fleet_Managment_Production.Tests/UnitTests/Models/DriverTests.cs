using Xunit;
using Fleet_Managment_Production.Models;
using Fleet_Managment_Production.Tests.Helpers;
using System;
using System.Linq;

namespace Fleet_Managment_Production.Tests.UnitTests.Models
{
    public class DriverTests
    {
        // ==========================================
        // FABRYKA
        // ==========================================
        private Driver CreateValidDriver()
        {
            return new Driver
            {
                FirstName = "Jan",
                LastName = "Kowalski",
                // PESEL: rocznik 1990, styczeń, 01 -> pełnoletni
                Pesel = "90010112345",
                Status = DriverStatus.Active,
                PhoneNumber = "123456789",
                Email = "jan.kowalski@test.pl"
            };
        }

        // ==========================================
        // TESTY WŁAŚCIWOŚCI OBLICZANYCH
        // ==========================================

        [Fact]
        public void FullName_ReturnsCombinedFirstAndLastName()
        {
            var driver = CreateValidDriver();
            driver.FirstName = "Anna";
            driver.LastName = "Nowak";

            Assert.Equal("Anna Nowak", driver.FullName);
        }

        // ==========================================
        // TESTY BIZNESOWE (Logika PESEL - Wiek)
        // ==========================================

        [Fact]
        public void Validate_DriverUnder18YearsOld_ReturnsValidationError()
        {
            var driver = CreateValidDriver();

            // Rocznik 2015 (Pełnoletność dopiero w 2033 roku)
            // Urodzony w styczniu (01) + 20 dla roczników 2000+ = 21. Dzień 05.
            driver.Pesel = "15210512345";

            var errors = ValidationHelper.ValidateModel(driver);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Driver.Pesel)));
            Assert.Contains(errors, e => e.ErrorMessage.Contains("18 lat"));
        }

        [Fact]
        public void Validate_PeselWithInvalidDate_ReturnsValidationError()
        {
            var driver = CreateValidDriver();

            // 90 rok, 02 miesiąc (Luty), 30 dzień -> Luty nigdy nie ma 30 dni!
            driver.Pesel = "90023012345";

            var errors = ValidationHelper.ValidateModel(driver);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Driver.Pesel)));
            Assert.Contains(errors, e => e.ErrorMessage.Contains("nieprawidłową datę"));
        }

        // ==========================================
        // TESTY FORMATÓW (Regex)
        // ==========================================

        [Theory]
        [InlineData("1234567890")]   // 10 znaków (za krótki)
        [InlineData("123456789012")] // 12 znaków (za długi)
        [InlineData("9001011234A")]  // Zawiera literę
        public void Pesel_InvalidFormat_FailsValidation(string invalidPesel)
        {
            var driver = CreateValidDriver();
            driver.Pesel = invalidPesel;

            var errors = ValidationHelper.ValidateModel(driver);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Driver.Pesel)));
        }

        [Theory]
        [InlineData("brak-emaila")]
        [InlineData("test@test")] // Brak końcówki np. .pl
        public void Email_InvalidFormat_FailsValidation(string invalidEmail)
        {
            var driver = CreateValidDriver();
            driver.Email = invalidEmail;

            var errors = ValidationHelper.ValidateModel(driver);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Driver.Email)));
        }

        [Theory]
        [InlineData("123")] // Za krótki (wymagane min. 9)
        [InlineData("12345678901234567")] // Za długi (wymagane max 15)
        [InlineData("test12345")] // Zawiera litery
        public void PhoneNumber_InvalidFormat_FailsValidation(string invalidPhone)
        {
            var driver = CreateValidDriver();
            driver.PhoneNumber = invalidPhone;

            var errors = ValidationHelper.ValidateModel(driver);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Driver.PhoneNumber)));
        }

        // ==========================================
        // HAPPY PATH
        // ==========================================

        [Fact]
        public void Driver_FullyValidModel_PassesAllValidation()
        {
            var driver = CreateValidDriver();

            var errors = ValidationHelper.ValidateModel(driver);

            Assert.Empty(errors);
        }
    }
}