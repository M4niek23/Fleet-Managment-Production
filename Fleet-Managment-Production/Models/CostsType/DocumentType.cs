using System.ComponentModel.DataAnnotations;

namespace Fleet_Managment_Production.Models.CostsType
{
    public enum DocumentType
    {
        [Display(Name = "Faktura kosztowa")]
        CostInvoice,
        [Display(Name = "Faktura przychodowa")]
        IncomeInvoice
    }
}
