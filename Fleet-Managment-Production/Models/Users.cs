using Microsoft.AspNetCore.Identity;

namespace Fleet_Managment_Production.Models
{
    public class Users : IdentityUser
    {
        public string FullName { get; set; }
    }
}
