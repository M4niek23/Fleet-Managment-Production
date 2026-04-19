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
            driver.Pesel = "15210512345";

            var errors = ValidationHelper.ValidateModel(driver);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Driver.Pesel)));
            Assert.Contains(errors, e => e.ErrorMessage.Contains("18 lat"));
        }

        [Fact]
        public void Validate_PeselWithInvalidDate_ReturnsValidationError()
        {
            var driver = CreateValidDriver();

            driver.Pesel = "90023012345";

            var errors = ValidationHelper.ValidateModel(driver);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Driver.Pesel)));
            Assert.Contains(errors, e => e.ErrorMessage.Contains("nieprawidłową datę"));
        }

        // ==========================================
        // TESTY FORMATÓW (Regex)
        // ==========================================

        [Theory]
        [InlineData("1234567890")]   
        [InlineData("123456789012")] 
        [InlineData("9001011234A")]  
        public void Pesel_InvalidFormat_FailsValidation(string invalidPesel)
        {
            var driver = CreateValidDriver();
            driver.Pesel = invalidPesel;

            var errors = ValidationHelper.ValidateModel(driver);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Driver.Pesel)));
        }

        [Theory]
        [InlineData("brak-emaila")]
        [InlineData("test@test")]
        public void Email_InvalidFormat_FailsValidation(string invalidEmail)
        {
            var driver = CreateValidDriver();
            driver.Email = invalidEmail;

            var errors = ValidationHelper.ValidateModel(driver);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Driver.Email)));
        }

        [Theory]
        [InlineData("123")]
        [InlineData("12345678901234567")]
        [InlineData("test12345")]
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