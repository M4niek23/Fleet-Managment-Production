using System.ComponentModel.DataAnnotations;

namespace Fleet_Managment_Production.ViewModels
{
    public class LoginVM
    {
        [Required(ErrorMessage = "Login jest wymagany.")]
        public string? Username { get; set; }

        [Required(ErrorMessage = "Hasło jest wymagane.")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Display(Name = "Zapamiętaj mnie")]
        public bool RememberMe { get; set; }
    }
}
