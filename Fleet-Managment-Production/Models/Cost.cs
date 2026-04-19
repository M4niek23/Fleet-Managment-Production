using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fleet_Managment_Production.Models
{
    public enum CostType
    {
        Paliwo,
        Serwis,
        Ubezpieczenie,
        Przegląd,
        Inne
    };
    public class Cost : IValidatableObject
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Pojazd jest wymagany.")]
        public int VehicleId { get; set; }
        public virtual Vehicle? Vehicle { get; set; }

        [Required(ErrorMessage = "Typ kosztu jest wymagany")]
        [EnumDataType(typeof(CostType), ErrorMessage = "Wybrano nieprawidłową kategorię.")]
        public CostType Type { get; set; }

        [Display(Name = "Opis")]
        [StringLength(500, ErrorMessage = "Opis nie może przekraczać 500 znaków.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Kwota jest wymagana.")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 1000000, ErrorMessage = "Kwota musi być większa od 0 i mniejsza niż 1 000 000.")]
        public decimal Amount { get; set; }

        [Display(Name = "Data")]
        [DataType(DataType.Date)]
        public DateTime Data { get; set; } = DateTime.Now;

        [Display(Name = "Ilość Paliwa (L)")]
        [Range(0.1, 2000, ErrorMessage = "Nieprawidłowa ilość litrów.")] 
        public double? Liters { get; set; }

        [Display(Name = "Przebieg (km)")]
        [Range(0, 2000000, ErrorMessage = "Przebieg musi być wartością dodatnią.")] 
        public int? CurrentOdometer { get; set; }

        [Display(Name = "Tankowanie do pełna")]
        public bool IsFullTank { get; set; } = false;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Type == CostType.Paliwo && (!Liters.HasValue || Liters <= 0))
            {
                yield return new ValidationResult(
                    "Dla kosztu paliwa musisz podać ilość litrów.",
                    new[] { nameof(Liters) });
            }

            if (Type != CostType.Paliwo && Liters.HasValue)
            {
                yield return new ValidationResult(
                    "Ilość litrów można podać tylko dla kosztu paliwa.",
                    new[] { nameof(Liters) });
            }

            if (Data > DateTime.Now.AddDays(1))
            {
                yield return new ValidationResult(
                    "Data kosztu nie może być z przyszłości.",
                    new[] { nameof(Data) });
            }
            if (Type != CostType.Paliwo && IsFullTank)
            {
                yield return new ValidationResult(
                    "Opcja 'Tankowanie do pełna' jest dostępna tylko dla kosztów paliwa.",
                    new[] { nameof(IsFullTank) });
            }
        }
    }
}
