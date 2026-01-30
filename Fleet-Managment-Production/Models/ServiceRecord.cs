using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fleet_Managment_Production.Models
{
    public enum ServiceStatus
    {
        [Display(Name = "Oczekujące")]
        Pending,
        [Display(Name = "W trakcie")]
        InProgress,
        [Display(Name = "Zakończone")]
        Completed,
        [Display(Name = "Anulowane")]
        Canceled
    }
    public class ServiceRecord
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Pojazd")]
        public int VehicleId { get; set; }

        [ForeignKey("VehicleId")]
        public virtual Vehicle Vehicle { get; set; }

        [Required(ErrorMessage = "Data wystąpienia usterki jest wymagana.")]
        [Display(Name = "Data wystąpienia usterki")]
        [DataType(DataType.Date)]
        public DateTime FaultDate { get; set; }

        [Required(ErrorMessage = "Planowana data naprawy jest wymagana.")]
        [Display(Name = "Planowna data naprawy")]
        public DateTime PlannedCompletionDate { get; set; }

        [Display(Name = "Data zakończenia naprawy")]
        [DataType(DataType.Date)]
        public DateTime? ActualCompletionDate { get; set; }

        [Required]
        [Display(Name = "Status")]
        public ServiceStatus Status { get; set; }

        [Required(ErrorMessage = "Koszt naprawy jest wymagany.")]
        [Display(Name = "Koszt naprawy")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal RepairCost { get; set; }

        [Required]
        [Display(Name = "Opis usterki")]
        [StringLength(1000)]
        public string FaultDescription { get; set; }
    }
}
