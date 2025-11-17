using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fleet_Managment_Production.Models
{
    public class Inspection
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage ="Data przeglądu jest wymagana.")]
        [Display(Name="Data przeglądu")]
        [DataType(DataType.Date)]
        public DateTime InspectionDate { get; set; }

        [Display(Name = "Opis")]
        public string? Description { get; set; }

        [Display(Name ="Przebieg (km)")]
        [Range(0,int.MaxValue,ErrorMessage = "Przebieg musi być liczbą dokładną")]
        public int? Mileage {  get; set; }

        [Display(Name = "Koszt")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, (double)decimal.MaxValue)]
        public decimal Cost { get; set; }

        [Required]
        [Display(Name = "Pojazd")]
        public int VehicleId { get; set; }

        [ForeignKey("VehicleId")]
        public Vehicle? Vehicle { get; set; }

        [Display(Name = "Wynik przeglądu")]
        public bool? IsResultPositive { get; set; } 

        [Display(Name = "Data ponownego przeglądu")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? NextInspectionDate { get; set; }

        [Display(Name = "Przegląd aktywny")]
        public bool IsActive { get; set; }
    }
}
