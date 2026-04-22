using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fleet_Managment_Production.Models
{
    public enum LicenseCategory
    {
        [Display(Name = "AM")] AM,
        [Display(Name = "A1")] A1,
        [Display(Name = "A2")] A2,
        [Display(Name = "A")] A,
        [Display(Name = "B1")] B1,
        [Display(Name = "B")] B,
        [Display(Name = "C1")] C1,
        [Display(Name = "C")] C,
        [Display(Name = "D1")] D1,
        [Display(Name = "D")] D,
        [Display(Name = "BE")] BE,
        [Display(Name = "C1E")] C1E,
        [Display(Name = "CE")] CE,
        [Display(Name = "D1E")] D1E,
        [Display(Name = "DE")] DE,
        [Display(Name = "T")] T
    }

    public enum DriverStatus
    {
        [Display(Name = "Pracuje")]
        Active,
        [Display(Name = "Urlop")]
        OnLeave,
        [Display(Name = "Nie pracuje")]
        Inactive,
    }

    [Index(nameof(Pesel), IsUnique = true)]
    public class Driver : IValidatableObject
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "Imię")]
        [Required, StringLength(50)]
        public string FirstName { get; set; } = null!;

        [Display(Name = "Nazwisko")]
        [Required, StringLength(50)]
        public string LastName { get; set; } = null!;

        [Display(Name = "PESEL")]
        [Required(ErrorMessage = "PESEL jest wymagany.")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "PESEL musi składać się z dokładnie 11 cyfr.")]
        public string Pesel { get; set; } = null!;

        [Display(Name = "Kategorie Prawa Jazdy")]
        public string? LicenseCategories { get; set; }

        [NotMapped]
        [Display(Name = "Kategorie Prawa Jazdy")]
        public List<LicenseCategory> SelectedCategories { get; set; } = new List<LicenseCategory>();

        [Display(Name = "Telefon")]
        [RegularExpression(@"^\+?[0-9\s\-]{9,15}$", ErrorMessage = "Niepoprawny format numeru telefonu. Wymagane od 9 do 15 znaków.")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Powiązane Konto Użytkownika")]
        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        [Display(Name = "Użytkownik Systemowy")]
        public Users? User { get; set; }

        [Required(ErrorMessage = "Status jest wymagany")]
        [Display(Name = "Status")]
        [EnumDataType(typeof(DriverStatus), ErrorMessage = "Nieprawidłowy status kierowcy.")]
        public DriverStatus Status { get; set; } = DriverStatus.Active;

        [Display(Name = "Email")]
        [RegularExpression(@"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$", ErrorMessage = "Niepoprawny format adresu e-mail.")]
        public string? Email { get; set; }

        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();

        public string FullName => $"{FirstName} {LastName}";
        public ICollection<Trip> Trips { get; set; } = new List<Trip>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrEmpty(Pesel) || Pesel.Length != 11 || !Pesel.All(char.IsDigit))
                yield break;

            int yy = int.Parse(Pesel.Substring(0, 2));
            int mm = int.Parse(Pesel.Substring(2, 2));
            int dd = int.Parse(Pesel.Substring(4, 2));

            int year, month;
            if (mm >= 81 && mm <= 92)      { year = 1800 + yy; month = mm - 80; }
            else if (mm >= 21 && mm <= 32) { year = 2000 + yy; month = mm - 20; }
            else if (mm >= 41 && mm <= 52) { year = 2100 + yy; month = mm - 40; }
            else                           { year = 1900 + yy; month = mm; }

            DateOnly birthDate;
            try
            {
                birthDate = new DateOnly(year, month, dd);
            }
            catch
            {
                yield return new ValidationResult(
                    "PESEL zawiera nieprawidłową datę urodzenia.",
                    new[] { nameof(Pesel) });
                yield break;
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            var age = today.Year - birthDate.Year;
            if (today < birthDate.AddYears(age)) age--;

            if (age < 18)
            {
                yield return new ValidationResult(
                    "Kierowca musi mieć ukończone 18 lat.",
                    new[] { nameof(Pesel) });
            }
        }
    }
}
