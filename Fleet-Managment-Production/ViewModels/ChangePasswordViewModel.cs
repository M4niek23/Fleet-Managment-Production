using System.ComponentModel.DataAnnotations;

namespace Fleet_Managment_Production.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Email jest wymagany.")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Hasła jest wymagane.")]
        [StringLength(40, MinimumLength = 8, ErrorMessage = "{0} musi znajdować się w {2} i mieć maksymalnie {1} znaków.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nowe hasło")]
        [Compare("ConfirmNewPassword", ErrorMessage = "Hasła do siebie nie pasują.")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Nowe hasło jest wymagane.")]
        [DataType(DataType.Password)]
        [Display(Name = "Potwierdź nowe hasło.")]
        public string ConfirmNewPassword { get; set; }
    }
}
