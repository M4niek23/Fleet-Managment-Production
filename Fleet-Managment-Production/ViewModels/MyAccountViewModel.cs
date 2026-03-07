using System.ComponentModel.DataAnnotations;

namespace Fleet_Managment_Production.ViewModels
{
    public class MyAccountViewModel
    {
        [Display(Name = "Imię i nazwisko")]
        public string FullName { get; set; }

        [Display(Name = "Adres Email")]
        public string Email { get; set; }

        [Display(Name = "Numer telefonu")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Uprawnienia")]
        public IList<string> Roles { get; set; }

        [Display(Name = "Wszystkie pojazdy")]
        public int TotalVehiclesCount { get; set; }

        [Display(Name = "Pojazdy w trasie")]
        public int VehiclesInUseCount { get; set; }

        [Display(Name = "Pojazdy w serwisie")]
        public int VehiclesInMaintenanceCount { get; set; }

        [Display(Name = "Przejechane kilometry (z tras)")]
        public int TotalDistanceDriven { get; set; }

        [Display(Name = "Całkowite koszty")]
        public decimal TotalCosts { get; set; }

        [Display(Name = "Trwające serwisy")]
        public int ActiveServicesCount { get; set; }
    }

}
