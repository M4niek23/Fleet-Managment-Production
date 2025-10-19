using System.ComponentModel.DataAnnotations;

namespace Fleet_Managment_Production.ViewModels
{
    public class RegisterVM
    {
        [Required]
        public string? Name { get; set; }
        
        [Required]
        [DataType(DataType.EmailAddress)]
        public string? Email { get; set; }
        
        [Required]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Compare("Password", ErrorMessage = "Hasła nie są identyczne.")]
        [Display(Name = "Potwierdź hasło")]
        public string? ConfirmPassword { get; set; }

        [DataType(DataType.MultilineText)]
        public string? Address { get; set; }
    }
}
