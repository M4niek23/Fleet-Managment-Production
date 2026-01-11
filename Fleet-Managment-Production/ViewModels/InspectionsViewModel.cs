using Fleet_Managment_Production.Models;

namespace Fleet_Managment_Production.ViewModels
{
    public class InspectionsViewModel
    {
        public List<Inspection> ActiveInspections { get; set; } = new List<Inspection>();
        public List<Inspection> NegativeInspections { get; set; } = new List<Inspection>();
        public List<Inspection> HistoricalInspections { get; set; } = new List<Inspection>();
    }
}
