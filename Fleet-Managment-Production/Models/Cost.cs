using Fleet_Managment_Production.Models.CostsType;
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
        [Column(TypeName = "decimal(18, 2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Kwota musi być dodatnia")]
        [Display(Name = "Kwota netto")]
        public decimal Kwota { get; set; }

        [Required]
        [Display(Name = "Stawka VAT")]
        public VatType Vat { get; set; }

        [Display(Name = "Kwota brutto")]
        public decimal KwotaBrutto
        {
            get
            {
                decimal stawka = (int)Vat > 0 ? (int)Vat : 0;
                return Kwota + (Kwota * (stawka / 100m));
            }
        }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Data dokumentu")]
        public DateTime Data { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Numer dokumentu jest wymagany")]
        [Display(Name = "Numer dokumentu")]
        [StringLength(50)]
        public string DocumentNumber { get; set; } = null!;

        [Required]
        [Display(Name = "Rodzaj dokumentu")]
        public DocumentType DocumentType { get; set; }

        public int VehicleId { get; set; }

        [ForeignKey(nameof(VehicleId))]
        public Vehicle? Vehicle { get; set; }
    }
}