using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fleet_Managment_Production.Models
{
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
    }
}