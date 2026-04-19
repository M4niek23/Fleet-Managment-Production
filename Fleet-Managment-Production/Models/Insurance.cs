using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Fleet_Managment_Production.Models
{
    public enum AcScope
    {
        [Display(Name = "Brak")]
        None,
        [Display(Name = "Mini AC")]
        Mini,
        [Display(Name = "Pełne AC")]
        Full
    }
    public class Insurance : IValidatableObject
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Proszę podać numer polisy")]
        [Display(Name = "Numer Polisy")]
        [StringLength(50, ErrorMessage = "Numer polisy nie może przekraczać 50 znaków.")]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Numer polisy może zawierać wyłącznie litery i cyfry, bez znaków specjalnych.")]
        public string PolicyNumber { get; set; }

        [Required(ErrorMessage = "Proszę podać nazwę ubezpieczyciela")]
        [StringLength(100, ErrorMessage = "Nazwa ubezpieczyciela jest za długa.")]
        [Display(Name = "Ubezpieczyciel")]
        public string InsurareName { get; set; }

        [Required(ErrorMessage = "Proszę podać datę rozpoczęcia")]
        [DataType(DataType.Date)]
        [Display(Name = "Data rozpoczęcia")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Proszę podać datę wygaśnięcia")]
        [DataType(DataType.Date)]
        [Display(Name = "Data wygaśnięcia")]
        public DateTime ExpiryDate { get; set; }

        private decimal _cost;

        [Required(ErrorMessage = "Proszę podać koszt ubezpieczenia")]
        [Display(Name = "Koszt")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.0, 1000000.0, ErrorMessage = "Koszt ubezpieczenia musi być wartością dodatnią.")] 
        public decimal Cost
        {
             get => _cost;
            set => _cost = Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        [Display(Name = "Zawiera OC")]
        public bool HasOc { get; set; } = true;

        [Required(ErrorMessage = "Proszę określić zakres AC.")]
        [Display(Name = "Zakres Autocasco (AC)")]
        [EnumDataType(typeof(AcScope), ErrorMessage = "Wybrano nieprawidłowy zakres AC.")]
        public AcScope AcScope { get; set; } = AcScope.None;

        [Display(Name = "Zawiera Assistance")]
        public bool HasAssistance { get; set; }

        [Display(Name = "Zawiera NNW")]
        public bool HasNNW { get; set; }

        [Display(Name = "Czy to jest aktywna polisa ?")]
        public bool IsCurrent { get; set; }

        [Display(Name = "Pojazd")]
        [Required(ErrorMessage = "Musisz wybrać pojazd z listy.")]
        public int? VehicleId { get; set; }

        [ValidateNever]
        [Display(Name = "Pojazd")]
        public Vehicle? Vehicle { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ExpiryDate <= StartDate)
            {
                yield return new ValidationResult(
                    "Data wygaśnięcia musi być późniejsza niż data rozpoczęcia.",
                    new[] { nameof(ExpiryDate) });
            }
            if (IsCurrent && ExpiryDate < DateTime.Today)
            {
                yield return new ValidationResult(
                    "Polisa nie może być oznaczona jako aktywna, jeśli data jej wygaśnięcia już minęła.",
                    new[] { nameof(IsCurrent) });
            }
        }
    }
}
