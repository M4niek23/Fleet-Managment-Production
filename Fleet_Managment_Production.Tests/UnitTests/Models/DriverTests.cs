using Xunit;
using Fleet_Managment_Production.Models;
using Fleet_Managment_Production.Tests.Helpers;
using System.Linq;

namespace Fleet_Managment_Production.Tests.UnitTests.Models
{
    public class DriverTests
    {
        private Driver CreateValidDriver()
        {
            return new Driver
            {
                FirstName = "Jan",
                LastName = "Kowalski",
                Pesel = "12345678901",
                Status = DriverStatus.Active,
                Email = "jan.kowalski@firma.pl",
                PhoneNumber = "123456789" 
            };
        }


        [Fact]
        public void Constructor_InitializesCollections_ToPreventNullReferences()
        {
            var driver = new Driver();

            Assert.NotNull(driver.Vehicles);
            Assert.NotNull(driver.Trips);
            Assert.NotNull(driver.SelectedCategories);
            Assert.Empty(driver.Vehicles);
            Assert.Empty(driver.Trips);
            Assert.Empty(driver.SelectedCategories);
        }

        [Fact]
        public void FullName_WithValidNames_ReturnsCorrectlyFormattedString()
        {
            var driver = CreateValidDriver();
            driver.FirstName = "Adam";
            driver.LastName = "Nowak";

            Assert.Equal("Adam Nowak", driver.FullName);
        }


        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void RequiredFields_WhenNullOrWhitespace_FailsValidation(string invalidValue)
        {
            // Arrange
            var driver1 = CreateValidDriver(); driver1.FirstName = invalidValue;
            var driver2 = CreateValidDriver(); driver2.LastName = invalidValue;

            // Act
            var errors1 = ValidationHelper.ValidateModel(driver1);
            var errors2 = ValidationHelper.ValidateModel(driver2);

            // Assert
            Assert.Contains(errors1, e => e.MemberNames.Contains(nameof(Driver.FirstName)));
            Assert.Contains(errors2, e => e.MemberNames.Contains(nameof(Driver.LastName)));
        }

        [Fact]
        public void StringLength_Exceeding50Chars_FailsValidation()
        {
            var driver = CreateValidDriver();
            var longString = new string('X', 51); 

            driver.FirstName = longString;
            driver.LastName = longString;

            var errors = ValidationHelper.ValidateModel(driver);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Driver.FirstName)));
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Driver.LastName)));
        }


        [Fact]
        public void OptionalFields_WhenNull_PassesValidation()
        {
            var driver = CreateValidDriver();
            driver.Email = null;
            driver.PhoneNumber = null;
            driver.UserId = null;
            driver.LicenseCategories = null;

            var errors = ValidationHelper.ValidateModel(driver);

            Assert.Empty(errors);
        }


        [Theory]
        [InlineData("tekst zamiast numeru")]
        [InlineData("123")]
        public void PhoneNumber_WithInvalidFormat_FailsValidation(string invalidPhone)
        {
            var driver = CreateValidDriver();
            driver.PhoneNumber = invalidPhone;

            var errors = ValidationHelper.ValidateModel(driver);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Driver.PhoneNumber)));
        }

        [Fact]
        public void PhoneNumber_ExceedingLength_FailsValidation()
        {
            var driver = CreateValidDriver();
            driver.PhoneNumber = "+48 123 456 789 000000";

            var errors = ValidationHelper.ValidateModel(driver);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Driver.PhoneNumber)));
        }


        [Fact]
        public void Status_WithInvalidEnumValue_FailsValidation()
        {
            var driver = CreateValidDriver();
            driver.Status = (DriverStatus)999;

            var errors = ValidationHelper.ValidateModel(driver);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Driver.Status)));
        }

        [Fact]
        public void Status_WithValidEnumValue_PassesValidation()
        {
            var driver = CreateValidDriver();
            driver.Status = DriverStatus.OnLeave; // Istniejący status

            var errors = ValidationHelper.ValidateModel(driver);

            Assert.DoesNotContain(errors, e => e.MemberNames.Contains(nameof(Driver.Status)));
        }


        [Theory]
        [InlineData("1234567890")]  
        [InlineData("123456789012")]  
        [InlineData("12345ABC890")]   
        public void Pesel_WithInvalidLengthOrCharacters_FailsValidation(string invalidPesel)
        {
            var driver = CreateValidDriver();
            driver.Pesel = invalidPesel;

            var errors = ValidationHelper.ValidateModel(driver);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Driver.Pesel)));
        }

        [Theory]
        [InlineData("test@test.pl")]
        [InlineData("user+123@gmail.com")]
        public void Email_WithValidFormats_PassesValidation(string validEmail)
        {
            var driver = CreateValidDriver();
            driver.Email = validEmail;

            var errors = ValidationHelper.ValidateModel(driver);

            Assert.DoesNotContain(errors, e => e.MemberNames.Contains(nameof(Driver.Email)));
        }

        [Theory]
        [InlineData("test")]
        [InlineData("test@")]
        [InlineData("@test.pl")]
        [InlineData("test space@domena.pl")]
        public void Email_WithInvalidFormats_FailsValidation(string invalidEmail)
        {
            var driver = CreateValidDriver();
            driver.Email = invalidEmail;

            var errors = ValidationHelper.ValidateModel(driver);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Driver.Email)));
        }
        [Fact]
        public void SelectedCategories_WhenModified_ShouldBeConsistentWithLicenseCategories()
        {
            // Arrange
            var driver = new Driver();
            var categories = new List<LicenseCategory> { LicenseCategory.B, LicenseCategory.C };

            // Act
            driver.SelectedCategories = categories;
            driver.LicenseCategories = string.Join(",", categories);

            // Assert
            Assert.Contains("B", driver.LicenseCategories);
            Assert.Contains("C", driver.LicenseCategories);
        }

        [Fact]
        public void Driver_CanBeAssociatedWithMultipleVehicles()
        {
            // Arrange
            var driver = new Driver();
            var v1 = new Vehicle { LicensePlate = "KR123" };
            var v2 = new Vehicle { LicensePlate = "WA456" };

            // Act
            driver.Vehicles.Add(v1);
            driver.Vehicles.Add(v2);

            // Assert
            Assert.Equal(2, driver.Vehicles.Count);
        }
    }
}