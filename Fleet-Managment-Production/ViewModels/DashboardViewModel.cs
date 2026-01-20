using Fleet_Managment_Production.Models;
using System.Collections.Generic;

namespace Fleet_Managment_Production.ViewModels
{
    public class DashboardViewModel
    {
        public List<Insurance> ExpiringInsurances { get; set; }

        // Dodajemy nową listę dla przeglądów
        public List<Inspection> ExpiringInspections { get; set; }
    }
}