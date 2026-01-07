using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fleet_Managment_Production.Models
{
    public class Trip
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "Pojazd")]
        public int VehicleId { get; set; }

        [ForeignKey("VehicleId")]
        public virtual Vehicle? Vehicle { get; set; }

        [Display(Name = "Kierowca")]
        public int DriverId { get; set; }

        [ForeignKey("DriverId")]
        public virtual Driver? Driver { get; set; }

        [Required]
        [Display(Name = "Data rozpoczęcia")]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [Display(Name = "Data zakończenia")]
        public DateTime? EndTime { get; set; }

        [Required]
        [Display(Name = "Miejsce startu")]
        public string StartLocation { get; set; }

        public double? StartLatitude {get; set; }
        public double? StartLongitude {get; set; }

        [Required]
        [Display(Name = "Miejsce docelowe")]
        public string EndLocation { get; set; }

        public double? EndLatitude {get; set; }
        public double? EndLongitude {get; set; }

        [Required]
        [Display(Name = "Licznik początkowy (km)")]
        public int StartOdometer { get; set; }

        [Display(Name = "Licznik końcowy (km)")]
        public int? EndOdometer { get; set; }

        [Display(Name = "Szacowany dystans (z mapy)")]
        public double? EstimatedDistanceKm { get; set; } 

        [Display(Name = "Opis / Cel")]
        public string? Description { get; set; }

        [Display(Name = "Rodzaj podróży")]
        public TripType TripType { get; set; }

        [NotMapped]
        public int RealDistance => (EndOdometer.HasValue && EndOdometer > StartOdometer)
            ? EndOdometer.Value - StartOdometer
            : 0;

    }
    public enum TripType
    {
        [Display(Name = "Służbowa")]
        Business,
        [Display(Name = "Prywatna")]
        Private,
        [Display(Name = "Serwisowa")]
        Service
    }
}
