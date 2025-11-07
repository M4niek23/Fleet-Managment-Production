using Fleet_Managment_Production.Models;

namespace Fleet_Managment_Production.ViewModels
{
    public class InspectionsViewModel
    {
        public List<Inspection> UpcomingInspections { get; set; } = new List<Inspection>();
        public List<Inspection> HistoricalInspections { get; set; } = new List<Inspection>();
    }
}
