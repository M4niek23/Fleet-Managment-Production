using System.ComponentModel.DataAnnotations;

// Upewnij się, że ta przestrzeń nazw pasuje do reszty Twoich modeli
namespace Fleet_Managment_Production.Models
{
    public enum FuelType
    {
        [Display(Name = "Benzyna (PB)")]
        Benzyna,

        [Display(Name = "Olej napędowy (ON)")]
        Diesel,

        [Display(Name = "Napęd hybrydowy (HEV/PHEV)")]
        Hybryda,

        [Display(Name = "Napęd elektryczny (BEV)")]
        Elektryk,

        [Display(Name = "Gaz (LPG/CNG)")]
        LPG
    }
}