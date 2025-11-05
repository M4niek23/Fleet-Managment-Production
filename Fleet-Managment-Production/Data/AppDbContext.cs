using AspNetCoreGeneratedDocument;
using Fleet_Managment_Production.Models;
using Fleet_Managment_Production.Models.VehicleTable;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Fleet_Managment_Production.Data
{
    public class AppDbContext : IdentityDbContext<Users, IdentityRole, string>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Vehicle> Vehicles;
        public DbSet<Insurance> Insurances;

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // Konfiguracja Users do Vehicles
            b.Entity<Users>()
            .HasMany(u => u.Vehicles)
            .WithOne(v => v.User)
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        }
       
    }
}