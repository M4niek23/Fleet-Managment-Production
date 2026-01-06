using System.ComponentModel.DataAnnotations;

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
}
