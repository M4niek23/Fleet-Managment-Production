using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fleet_Managment_Production.Models
{
    public class Service
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Musisz wybrać pojazd.")]
        public int VehicleId { get; set; }

        [ForeignKey(nameof(VehicleId))]
        public Vehicle? Vehicle { get; set; }

        [Display(Name = "Opis usterki/serwisu"), Required(ErrorMessage = "Pole opisu usterki jest wymagane.")]
        public string Description { get; set; } = null!;

        [Required(ErrorMessage ="Koszt jest wymagany.")]
        [Display(Name = "Koszt"), Range(0, double.MaxValue)]
        [Precision(18,2)]
        public decimal Cost { get; set; }

        [Display(Name = "Data przyjęcia"), DataType(DataType.Date)]
        [Required(ErrorMessage = "Data przyjęcia jest wymagana.")]
        public DateTime EntryDate { get; set; } = DateTime.Now;

        [Display(Name = "Planowane zakończenie"), DataType(DataType.Date)]
        public DateTime? PlannedEndDate { get; set; }

        [Display(Name = "Rzeczywiste zakończenie"), DataType(DataType.Date)]
        public DateTime? ActualEndDate { get; set; }

        [Display(Name = "Czy zakończono?")]
        public bool IsFinished => ActualEndDate.HasValue;


    }
}
