using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fleet_Managment_Production.Models
{
    public class Cost
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Typ kosztu")]
        public CostType Type { get; set; }

        [Required(ErrorMessage = "Opis jest wymagany")]
        [StringLength(250)]
        public string Opis { get; set; } = null!;

        [Required(ErrorMessage = "Kwota jest wymagana")]
        [Column(TypeName = "decimal(18, 2)")] // Ważne dla kwot pieniężnych
        [Range(0.01, double.MaxValue, ErrorMessage = "Kwota musi być dodatnia")]
        [Display(Name = "Kwota")]
        public decimal Kwota { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Data poniesienia kosztu")]
        public DateTime Data { get; set; } = DateTime.Now;

        public int VehicleId { get; set; }

        [ForeignKey(nameof(VehicleId))]
        public Vehicle? Vehicle { get; set; }

    }
}
