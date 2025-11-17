using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;


namespace Fleet_Managment_Production.Models
{
    [Index(nameof(VIN), IsUnique = true)]
    [Index(nameof(LicensePlate), IsUnique = true)]
    public class Vehicle
    {
        public int VehicleId { get; set; }


        public VehicleStatus Status { get; set; } = VehicleStatus.Available;


        [Display(Name = "Marka"), Required, StringLength(50)]
        public string Make { get; set; } = null!;


        [Required, StringLength(50)]
        public string Model { get; set; } = null!;


        [Display(Name = "Typ paliwa")]
        public FuelType FuelType { get; set; }


        [Display(Name = "Rok produkcji"), Range(1886, 2100)]
        public int ProductionYear { get; set; }


        [Display(Name = "Numer rejestracyjny"), StringLength(20)]
        public string? LicensePlate { get; set; }


        [StringLength(17, MinimumLength = 17)]
        [RegularExpression(@"^[A-HJ-NPR-Z0-9]{17}$")] // bez I,O,Q
        public string? VIN { get; set; }


        [Display(Name = "Aktualny przebieg"), Range(0, int.MaxValue)]
        public int CurrentKm { get; set; }


        public string? UserId { get; set; }


        [ForeignKey(nameof(UserId))]
        public Users? User { get; set; }

        public ICollection<Inspection> Inspections { get; set; } = new List<Inspection>();
        public ICollection<Insurance> Insurances { get; set; } = new List<Insurance>();
        public ICollection<Cost> Costs { get; set; } = new List<Cost>();
    }
}