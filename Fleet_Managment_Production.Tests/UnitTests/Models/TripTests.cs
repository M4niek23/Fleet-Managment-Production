using Xunit;
using Fleet_Managment_Production.Models;
using Fleet_Managment_Production.Tests.Helpers;
using System;
using System.Linq;

namespace Fleet_Managment_Production.Tests.UnitTests.Models
{
    public class TripTests
    {
        // ==========================================
        // FABRYKA
        // ==========================================
        private Trip CreateValidTrip()
        {
            return new Trip
            {
                VehicleId = 1,
                DriverId = 1,
                StartDate = DateTime.Now,
                EndTime = DateTime.Now.AddHours(3),
                StartLocation = "Warszawa",
                EndLocation = "Poznań",
                StartOdometer = 120000,
                EndOdometer = 120320,
                TripType = TripType.Business
            };
        }

        // ==========================================
        // TESTY WŁAŚCIWOŚCI OBLICZANYCH (RealDistance)
        // ==========================================

        [Fact]
        public void RealDistance_CalculatedCorrectly_WhenValidDataProvided()
        {
            var trip = CreateValidTrip();
            Assert.Equal(320, trip.RealDistance);
        }

        [Fact]
        public void RealDistance_ReturnsZero_WhenTripIsOngoing()
        {
            var trip = CreateValidTrip();
            trip.EndOdometer = null;

            Assert.Equal(0, trip.RealDistance);
        }

        // ==========================================
        // TESTY BIZNESOWE (Czas i Fizyka)
        // ==========================================

        [Fact]
        public void Validate_EndTimeBeforeStartDate_ReturnsValidationError()
        {
            var trip = CreateValidTrip();
            trip.StartDate = DateTime.Now;
            trip.EndTime = DateTime.Now.AddHours(-1);

            var errors = ValidationHelper.ValidateModel(trip);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Trip.EndTime)));
        }

        [Fact]
        public void Validate_EndOdometerLessThanStartOdometer_ReturnsValidationError()
        {
            var trip = CreateValidTrip();
            trip.StartOdometer = 50000;
            trip.EndOdometer = 49500;

            var errors = ValidationHelper.ValidateModel(trip);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Trip.EndOdometer)));
        }

        // ==========================================
        // TESTY OCHRONY BAZY DANYCH I KLUCZY
        // ==========================================

        [Fact]
        public void ForeignKeys_NotAssigned_FailsValidation()
        {
            var trip = CreateValidTrip();
            trip.VehicleId = 0; 
            trip.DriverId = 0;  

            var errors = ValidationHelper.ValidateModel(trip);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Trip.VehicleId)));
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Trip.DriverId)));
        }

        [Fact]
        public void Enum_WithInvalidValue_FailsValidation()
        {
            var trip = CreateValidTrip();
            trip.TripType = (TripType)99;

            var errors = ValidationHelper.ValidateModel(trip);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Trip.TripType)));
        }

        [Fact]
        public void GPSCoordinates_OutofBounds_FailsValidation()
        {
            var trip = CreateValidTrip();
            trip.StartLatitude = 95.0; 
            trip.StartLongitude = 185.0;

            var errors = ValidationHelper.ValidateModel(trip);

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Trip.StartLatitude)));
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Trip.StartLongitude)));
        }

        // ==========================================
        // HAPPY PATH
        // ==========================================

        [Fact]
        public void Trip_FullyValidModel_PassesAllValidation()
        {
            var trip = CreateValidTrip();

            var errors = ValidationHelper.ValidateModel(trip);

            Assert.Empty(errors);
        }
    }
}