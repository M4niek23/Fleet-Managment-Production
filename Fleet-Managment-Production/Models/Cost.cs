using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fleet_Managment_Production.Models
{
    public enum CostType
    {
        Paliwo,
        Serwis,
        Ubezpieczenie,
        Przegląd,
        Inne
    };
    public class Cost
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "Pojazd")]
        public int VehicleId { get; set; }
        public virtual Vehicle? Vehicle { get; set; }

        [Required(ErrorMessage = "Typ kosztu jest wymagany")]
        [Display(Name = "Kategoria")]
        public CostType Type { get; set; } 

        [Display(Name = "Opis")]
        
        public string? Description { get; set; }

        [Display(Name = "Kwota")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 1000000, ErrorMessage = "Kwota musi być większa od 0")]
        public decimal Amount { get; set; }

        [Display(Name = "Data")]
        [DataType(DataType.Date)]
        public DateTime Data { get; set; }

        [Display(Name = "Ilość Paliwa (L)")]
        public double? Liters { get; set; }

        [Display(Name = "Przebieg (km)")]
        public int? CurrentOdometer { get; set; }

        [Display(Name = "Tankowanie do pełna")]
        public bool IsFullTank { get; set; } = false;
    }
}
