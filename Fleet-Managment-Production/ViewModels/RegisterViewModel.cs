using System.ComponentModel.DataAnnotations;

namespace Fleet_Managment_Production.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Nazwa jest wymagana.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email jest wymagany.")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Hasło jest wymagane.")]
        [StringLength(40, MinimumLength = 8, ErrorMessage = "{0} musi znajdować się w {2} i mieć maksymalnie {1} znaków.")]
        [DataType(DataType.Password)]
        [Compare("ConfirmPassword", ErrorMessage = "Hasła do siebie nie pasują.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Potwierdzenie nowego hasła jest wymagane.")]
        [DataType(DataType.Password)]
        [Display(Name = "Potwierdź hasło")]
        public string ConfirmPassword { get; set; }

    }
}
