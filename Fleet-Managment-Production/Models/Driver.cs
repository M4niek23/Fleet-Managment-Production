using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fleet_Managment_Production.Models
{
    public class Driver : IValidatableObject
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "Imię"), Required, StringLength(50)]
        public string FirstName { get; set; } = null!;

        [Display(Name = "Nazwisko"), Required, StringLength(50)]
        public string LastName { get; set; } = null!;

        [Display(Name = "Obcokrajowiec")]
        public bool IsForeigner { get; set; }

        [Display(Name = "PESEL"), StringLength(11)]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "PESEL musi składać się z 11 cyfr.")]
        public string? PESEL { get; set; }

        [Display(Name = "Data urodzenia"), Required,DataType(DataType.Date)]
        public DateTime DateOfBirth {get; set; }

        [Display(Name = "Numer prawa jazdy"), Required, StringLength(20)]
        public string LicenseNumber { get; set; } = null!;

        [Display(Name = "Kategoria/e prawa jazdy")]
        [Required]
        public List<LicenseCategory> LicenseCategories { get; set; }

        [Display(Name = "Data ważności prawa jazdy"), Required, DataType(DataType.Date)]
        public DateTime LicenseExpiryDate { get; set; }

        [Display(Name = "Data ważności badań lekarskich"), DataType(DataType.Date)]
        public DateTime? MedicalExamExpiryDate { get; set; }

        public string UserId { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        [ValidateNever]
        public Users? User { get; set; }

        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if(!IsForeigner)
            {
                if(string.IsNullOrWhiteSpace(PESEL))
                {
                    yield return new ValidationResult(
                        "PESEL jest wymagany dla kierowców będących obywatelami Polski.",
                        new[] { nameof(PESEL) });
                }
                else if (PESEL!.Length != 11 || !long.TryParse(PESEL, out _))
                {
                    yield return new ValidationResult(
                        "PESEL musi składać się z 11 cyfr.",
                        new[] { nameof(PESEL) });
                }
            }
        }

    }   
}
