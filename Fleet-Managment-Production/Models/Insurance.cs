using Fleet_Managment_Production.Models.VehicleTable;
using Microsoft.Identity.Client;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fleet_Managment_Production.Models
{
    public enum AcScope
    {
        [Display(Name = "Brak")]
        None,
        [Display(Name = "Mini AC")]
        Mini,
        [Display(Name = "Pełne AC")]
        Full
    }
    public class Insurance
    {
        [Key]   
        public int Id { get; set; }
        [Required(ErrorMessage = "Proszę podać numer polisy")]
        [Display(Name = "Numer Polisy")]
        public string PolicyNumber { get; set; }

        [Display(Name = "Ubezpieczyciel")]
        public string InsurareName { get; set; }

        [Required(ErrorMessage = "Proszę podać datę rozpoczęcia")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString ="{0:yyyy-MM-dd}",ApplyFormatInEditMode = true)]
        [Display(Name = "Data rozpoczęcia")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage ="Proszę podać datę wygaśnięcia")]
        [DataType (DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Data wygaśnięcia")]
        public DateTime ExpiryDate { get; set; }

        [Required(ErrorMessage = "Proszę podać koszt ubezpieczenia.")]
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Koszt (PLN)")]
        public decimal Cost { get; set; }

        [Display(Name = "Zawiera OC")]
        public bool HasOc { get; set; } = true;

        [Required(ErrorMessage ="Proszę określić zakres AC.")]
        [Display(Name = "Zakres Autocasco (AC)")]
        public AcScope AcScope { get; set; } = AcScope.None;

        [Display(Name ="Zawiera Assistance")]
        public bool HasAssistance { get; set; }

        [Display(Name = "Zawiera NNW")]
        public bool HasNNW { get; set; }

        [Display(Name = "Czy to jest aktywna polisa ?")]
        public bool IsCurrent { get; set; }
        
        [Display(Name="Pojazd")]
        public int VehicleId { get; set; }
        public Vehicle Vehicle { get; set; }
    }
}
