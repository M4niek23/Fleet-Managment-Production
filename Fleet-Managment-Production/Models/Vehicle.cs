using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore; 
using Fleet_Managment_Production.Models;


namespace Fleet_Managment_Production.Models.VehicleTable
{
    [Index(nameof(VIN), IsUnique = true)]
    [Index(nameof(LicensePlate), IsUnique = true)]
    public class Vehicle
    {
        public int VehicleId { get; set; }


        public VehicleStatus Status { get; set; } = VehicleStatus.Available;


        [Required, StringLength(50)]
        public string Make { get; set; } = null!;


        [Required, StringLength(50)]
        public string Model { get; set; } = null!;


        [StringLength(20)]
        public string? FuelType { get; set; }


        [Range(1886, 2100)]
        public int ProductionYear { get; set; }


        [StringLength(20)]
        public string? LicensePlate { get; set; }


        [StringLength(17, MinimumLength = 17)]
        [RegularExpression(@"^[A-HJ-NPR-Z0-9]{17}$")] // bez I,O,Q
        public string? VIN { get; set; }


        [Range(0, int.MaxValue)]
        public int CurrentKm { get; set; }


        public string? UserId { get; set; }


        [ForeignKey(nameof(UserId))]
        public Users? User { get; set; }

        public virtual ICollection<Insurance> Insurances { get; set; } = new List<Insurance>();

    }
}