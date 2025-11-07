using System.Collections.Generic;


namespace Fleet_Managment_Production.Models
{
    public class DashboardViewModel
    {
        // Będziemy tu trzymać listę polis, które wkrótce wygasną
        public List<Insurance> ExpiringInsurances { get; set; }
    }
}