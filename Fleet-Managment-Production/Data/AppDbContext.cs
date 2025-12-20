using AspNetCoreGeneratedDocument;
using Fleet_Managment_Production.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Fleet_Managment_Production.Data
{
    public class AppDbContext : IdentityDbContext<Users, IdentityRole, string>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Insurance> Insurances { get; set; }
        public DbSet<Inspection> Inspections { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBilder)
        {
            base.OnModelCreating(modelBilder);

            // Konfiguracja Users do Vehicles
            modelBilder.Entity<Users>()
            .HasMany(u => u.Vehicles)
            .WithOne(v => v.User)
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.SetNull);

            modelBilder.Entity<Vehicle>().ToTable("Vehicles");
            modelBilder.Entity<Insurance>().ToTable("Insurances");
            modelBilder.Entity<Vehicle>().HasKey(v => v.VehicleId);

            //Konfiguracja Vehicles do Inspection
            modelBilder.Entity<Inspection>()
                .HasOne(i => i.Vehicle)
                .WithMany(v => v.Inspections)
                .HasForeignKey(i => i.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);

        }
        

       
    }
}