using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fleet_Managment_Production.Models
{
    public class Service : IValidatableObject
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Musisz wybrać pojazd.")]
        [Range(1, int.MaxValue, ErrorMessage = "Musisz przypisać pojazd z listy.")]
        public int VehicleId { get; set; }

        [ForeignKey(nameof(VehicleId))]
        public Vehicle? Vehicle { get; set; }

        [Display(Name = "Opis usterki/serwisu")]
        [Required(ErrorMessage = "Pole opisu usterki jest wymagane.")]
        [StringLength(1000, ErrorMessage = "Opis nie może przekraczać 1000 znaków.")]
        public string Description { get; set; } = null!;

        [Required(ErrorMessage = "Koszt jest wymagany.")]
        [Display(Name = "Koszt")]
        [Range(0.0, 1000000.0, ErrorMessage = "Koszt musi być wartością pomiędzy 0 a 1 000 000.")]
        [Precision(18, 2)]
        public decimal Cost { get; set; }

        [Display(Name = "Data przyjęcia"), DataType(DataType.Date)]
        [Required(ErrorMessage = "Data przyjęcia jest wymagana.")]
        public DateTime EntryDate { get; set; } = DateTime.Now;

        [Display(Name = "Planowane zakończenie"), DataType(DataType.Date)]
        public DateTime? PlannedEndDate { get; set; }

        [Display(Name = "Rzeczywiste zakończenie"), DataType(DataType.Date)]
        public DateTime? ActualEndDate { get; set; }

        [Display(Name = "Czy zakończono?")]
        public bool IsFinished => ActualEndDate.HasValue;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (PlannedEndDate.HasValue && PlannedEndDate.Value < EntryDate)
            {
                yield return new ValidationResult(
                    "Planowana data zakończenia nie może być wcześniejsza niż data przyjęcia.",
                    new[] { nameof(PlannedEndDate) });
            }

            if (ActualEndDate.HasValue && ActualEndDate.Value < EntryDate)
            {
                yield return new ValidationResult(
                    "Rzeczywista data zakończenia nie może być wcześniejsza niż data przyjęcia.",
                    new[] { nameof(ActualEndDate) });
            }
        }
    }
}
