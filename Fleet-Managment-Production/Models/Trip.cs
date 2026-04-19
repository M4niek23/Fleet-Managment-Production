using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fleet_Managment_Production.Models
{
    public enum TripType
    {
        [Display(Name = "Służbowa")]
        Business,
        [Display(Name = "Prywatna")]
        Private,
        [Display(Name = "Serwisowa")]
        Service
    }

    public class Trip : IValidatableObject 
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Musisz wybrać pojazd.")]
        [Range(1, int.MaxValue, ErrorMessage = "Musisz przypisać pojazd z listy.")] 
        [Display(Name = "Pojazd")]
        public int VehicleId { get; set; }

        [ForeignKey("VehicleId")]
        public virtual Vehicle? Vehicle { get; set; }

        [Required(ErrorMessage = "Musisz wybrać kierowcę.")]
        [Range(1, int.MaxValue, ErrorMessage = "Musisz przypisać kierowcę z listy.")]
        [Display(Name = "Kierowca")]
        public int DriverId { get; set; }

        [ForeignKey("DriverId")]
        public virtual Driver? Driver { get; set; }

        [Required(ErrorMessage = "Data rozpoczęcia jest wymagana.")]
        [Display(Name = "Data rozpoczęcia")]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [Display(Name = "Data zakończenia")]
        public DateTime? EndTime { get; set; }

        [Required(ErrorMessage = "Miejsce startu jest wymagane.")]
        [StringLength(200, ErrorMessage = "Nazwa lokalizacji początkowej jest za długa.")] 
        [Display(Name = "Miejsce startu")]
        public string StartLocation { get; set; } = null!;

        [Range(-90.0, 90.0, ErrorMessage = "Nieprawidłowa szerokość geograficzna.")]
        public double? StartLatitude { get; set; }

        [Range(-180.0, 180.0, ErrorMessage = "Nieprawidłowa długość geograficzna.")]
        public double? StartLongitude { get; set; }

        [Required(ErrorMessage = "Miejsce docelowe jest wymagane.")]
        [StringLength(200, ErrorMessage = "Nazwa lokalizacji docelowej jest za długa.")]
        [Display(Name = "Miejsce docelowe")]
        public string EndLocation { get; set; } = null!;

        [Range(-90.0, 90.0)]
        public double? EndLatitude { get; set; }

        [Range(-180.0, 180.0)]
        public double? EndLongitude { get; set; }

        [Required(ErrorMessage = "Licznik początkowy jest wymagany.")]
        [Range(0, 2000000, ErrorMessage = "Licznik musi być wartością dodatnią.")]
        [Display(Name = "Licznik początkowy (km)")]
        public int StartOdometer { get; set; }

        [Display(Name = "Licznik końcowy (km)")]
        [Range(0, 2000000, ErrorMessage = "Licznik musi być wartością dodatnią.")]
        public int? EndOdometer { get; set; }

        [Display(Name = "Szacowany dystans (z mapy)")]
        [Range(0.0, 50000.0)]
        public double? EstimatedDistanceKm { get; set; }

        [Display(Name = "Opis / Cel")]
        [StringLength(1000, ErrorMessage = "Opis jest za długi.")]
        public string? Description { get; set; }

        [Display(Name = "Rodzaj podróży")]
        [EnumDataType(typeof(TripType), ErrorMessage = "Nieprawidłowy rodzaj podróży.")] 
        public TripType TripType { get; set; }

        [NotMapped]
        public int RealDistance => (EndOdometer.HasValue && EndOdometer > StartOdometer)
            ? EndOdometer.Value - StartOdometer
            : 0;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndTime.HasValue && EndTime.Value < StartDate)
            {
                yield return new ValidationResult(
                    "Data zakończenia nie może być wcześniejsza niż data rozpoczęcia.",
                    new[] { nameof(EndTime) });
            }

            if (EndOdometer.HasValue && EndOdometer.Value < StartOdometer)
            {
                yield return new ValidationResult(
                    "Końcowy stan licznika nie może być mniejszy niż początkowy.",
                    new[] { nameof(EndOdometer) });
            }
        }
    }
}