using System.ComponentModel.DataAnnotations;

namespace Fleet_Managment_Production.ViewModels
{
    public enum ReportType
    {
        [Display(Name = "Całkowite koszty utrzymania pojazdów")]
        TCO,
        [Display(Name = "Spalanie i koszty paliwa")]
        Fuel,
        [Display(Name = "Awaryjność i serwis")]
        Service,
        [Display(Name = "Aktywność kierowców")]
        DriverActivity,
        [Display(Name = "Alerty (Kończące się terminy)")] // Poprawiono brakujący nawias ")" na końcu
        Alerts
    }

    public class ReportsViewModel
    {
        [Display(Name = "Typ raportu")]
        public ReportType SelectedReport { get; set; }

        [Display(Name = "Data od")]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Data do")]
        public DateTime? EndDate { get; set; }

        public List<TcoReportItem> TcoData { get; set; } = new();
        public List<FuelReportItem> FuelData { get; set; } = new();
        public List<ServiceReportItem> ServiceData { get; set; } = new();
        public List<DriverActivityReportItem> DriverActivityData { get; set; } = new();
        public List<AlertsReportItem> AlertsData { get; set; } = new();
    }

    public class TcoReportItem
    {
        public string VehicleName { get; set; }
        public string LicensePlate { get; set; }
        public decimal FuelCost { get; set; }
        public decimal ServiceCost { get; set; }
        public decimal TotalCost => FuelCost + ServiceCost;
    }

    public class FuelReportItem
    {
        public string VehicleName { get; set; }
        public string LicensePlate { get; set; }
        public double TotalLiters { get; set; }
        public int DistanceTraveled { get; set; }
        public decimal AverageConsumption => DistanceTraveled > 0 ? (decimal)(TotalLiters / DistanceTraveled * 100) : 0;
    }

    public class ServiceReportItem
    {
        public string VehicleName { get; set; }
        public string LicensePlate { get; set; }
        public int ServiceCount { get; set; }
        public decimal TotalServiceCost { get; set; }
    }

    public class DriverActivityReportItem
    {
        public string DriverName { get; set; }
        public int TripsCount { get; set; }
        public int TotalDistance { get; set; }
    }

    public class AlertsReportItem
    {
        public string VehicleName { get; set; }
        public string LicensePlate { get; set; }
        public string AlertType { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int DaysLeft => (ExpiryDate - DateTime.Today).Days;
    }
}