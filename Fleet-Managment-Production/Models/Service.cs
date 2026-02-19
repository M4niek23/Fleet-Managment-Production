using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fleet_Managment_Production.Models
{
    public class Service
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int VehicleId { get; set; }

        [ForeignKey(nameof(VehicleId))]
        public Vehicle? Vehicle { get; set; }

        [Display(Name = "Opis usterki/serwisu"), Required]
        public string Description { get; set; } = null!;

        [Display(Name = "Koszt"), Range(0, double.MaxValue)]
        public decimal Cost { get; set; }

        [Display(Name = "Data przyjęcia"), DataType(DataType.Date)]
        public DateTime EntryDate { get; set; } = DateTime.Now;

        [Display(Name = "Planowane zakończenie"), DataType(DataType.Date)]
        public DateTime? PlannedEndDate { get; set; }

        [Display(Name = "Rzeczywiste zakończenie"), DataType(DataType.Date)]
        public DateTime? ActualEndDate { get; set; }

        [Display(Name = "Czy zakończono?")]
        public bool IsFinished => ActualEndDate.HasValue;

    }
}
