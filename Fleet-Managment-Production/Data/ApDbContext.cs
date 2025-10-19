using Fleet_Managment_Production.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace Fleet_Managment_Production.Data
{
    public class ApDbContext:IdentityDbContext<AppUser>
    {
        public ApDbContext(DbContextOptions<ApDbContext> options) : base(options)
        {
        }
    }
}
