using System.ComponentModel.DataAnnotations;

namespace Fleet_Managment_Production.Models.CostsType
{
    public enum VatType
    {
        [Display(Name = "23%")]
        Vat23 = 23,
        [Display(Name = "8%")]
        Vat8 = 8,
        [Display(Name = "5%")]
        Vat5 = 5,
        [Display(Name = "0%")]
        Vat0 = 0,
        [Display(Name = "Zwolniony z podatku")]
        Exempt = -1
    }
}
