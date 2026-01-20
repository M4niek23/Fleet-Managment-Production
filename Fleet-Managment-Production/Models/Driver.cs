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
    [Index(nameof(Pesel), IsUnique = true)]
    public class Driver
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
        [Required, StringLength(11)]
        public string Pesel { get; set; } = null!;

        [Display(Name = "Kategoria")]
        public LicenseCategory LicenseCategory { get; set; }

        [Display(Name = "Telefon")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Powiązane Konto Użytkownika")]
        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        [Display(Name = "Użytkownik Systemowy")]
        public Users? User { get; set; }

        [Display(Name = "Email")]
        [EmailAddress]
        public string? Email { get; set; }

        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();

        public string FullName => $"{FirstName} {LastName}";
        public ICollection<Trip> Trips { get; set; } = new List<Trip>();
    }
}