using Fleet_Managment_Production.Models; // Użyj swojej przestrzeni nazw dla modelu 'Insurance'
using System.Collections.Generic;

// Upewnij się, że ta przestrzeń nazw jest poprawna
namespace Fleet_Managment_Production.Models.Home
{
    public class DashboardViewModel
    {
        // Będziemy tu trzymać listę polis, które wkrótce wygasną
        public List<Insurance> ExpiringInsurances { get; set; }
    }
}