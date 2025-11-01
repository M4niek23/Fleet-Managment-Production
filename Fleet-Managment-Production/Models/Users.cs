using Fleet_Managment_Production.Models.VehicleTable;
using Microsoft.AspNetCore.Identity;

namespace Fleet_Managment_Production.Models
{
    public class Users : IdentityUser
    {
        public string FullName { get; set; }
        public ICollection<Vehicle> Vehicles { get; set; }
    }
}
