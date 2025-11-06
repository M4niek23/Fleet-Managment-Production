using System.ComponentModel.DataAnnotations;

namespace Fleet_Managment_Production.Models
{
    // W pliku enum dla Statusu
    public enum VehicleStatus
    {
        [Display(Name = "Dostępny")]
        Available,

        [Display(Name = "W użyciu")]
        InUse,

        [Display(Name = "W serwisie")]
        InMaintenance,

        [Display(Name = "Wycofany")]
        Decommissioned
    }
}
