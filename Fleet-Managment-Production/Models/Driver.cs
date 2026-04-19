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
            if (!string.IsNullOrEmpty(Pesel) && Pesel.Length == 11 && Pesel.All(char.IsDigit))
            {
                int year = int.Parse(Pesel.Substring(0, 2));
                int month = int.Parse(Pesel.Substring(2, 2));
                int day = int.Parse(Pesel.Substring(4, 2));

                // Dekodowanie stulecia (PESEL po 2000 roku ma dodane 20 do miesiąca)
                if (month > 20 && month < 33)
                {
                    year += 2000;
                    month -= 20;
                }
                else if (month > 0 && month < 13)
                {
                    year += 1900;
                }

                string? peselErrorMessage = null; // Zmienna pomocnicza na błąd

                try
                {
                    DateTime birthDate = new DateTime(year, month, day);

                    // Sprawdzenie wieku
                    if (birthDate > DateTime.Today.AddYears(-18))
                    {
                        peselErrorMessage = "Kierowca musi mieć ukończone 18 lat.";
                    }
                }
                catch
                {
                    // Wyrzuci błąd, jeśli PESEL zawiera np. 31 lutego
                    peselErrorMessage = "Numer PESEL zawiera nieprawidłową datę urodzenia.";
                }

                // yield return WYKONUJEMY POZA BLOKIEM TRY-CATCH
                if (peselErrorMessage != null)
                {
                    yield return new ValidationResult(peselErrorMessage, new[] { nameof(Pesel) });
                }
            }
        }
    }
}