using System.ComponentModel.DataAnnotations;

namespace Fleet_Managment_Production.ViewModels
{
    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "Podanie adresu e-mail jest wymagane.")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy format adresu e-mail")]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Podanie nowego hasła jest wymagane.")]
        [StringLength(100, ErrorMessage = "Hasło musi mieć co najmniej {2} znaków.", MinimumLength = 8)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", ErrorMessage = "Hasło musi zawierać znak alfanumeryczny, cyfrę i dużą literę.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Potwierdzenie hasła jest wymagane.")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Hasła nie są zgodne.")]
        public string ConfirmPassword { get; set; } = string.Empty;

    }
}
