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
        public DbSet<Cost> Costs { get; set; }
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
            modelBilder.Entity<Cost>().ToTable("Costs");

            //Konfiguracja Vehicles do Inspection
            modelBilder.Entity<Inspection>()
                .HasOne(i => i.Vehicle)
                .WithMany(v => v.Inspections)
                .HasForeignKey(i => i.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);

            //Konfiguracja relacji Vehicle do Koszty (jeden do wielu)
            modelBilder.Entity<Cost>()
                .HasOne(c => c.Vehicle)
                .WithMany(v => v.Costs)
                .HasForeignKey(c => c.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);
        }
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SynchronizationCosts();
            return await base.SaveChangesAsync(cancellationToken);
        }
        public override int SaveChanges()
        {
            SynchronizationCosts();
            return base.SaveChanges();
        }
        private void SynchronizationCosts()
        {
            var addedEntries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added);

            foreach (var entry in addedEntries.ToList()) 
            {
                if(entry.Entity is Inspection inspection)
                {
                    var cost = new Cost
                    {
                        VehicleId = inspection.VehicleId,
                        Type = CostType.Przegląd,
                        Opis = $"Przegląd: {inspection.Description ?? "Brak opisu"}",
                        Kwota = inspection.Cost,
                        Data = inspection.InspectionDate,
                    };
                    Costs.Add(cost);
                }
                else if (entry.Entity is Insurance insurance)
                {
                    if(insurance.VehicleId.HasValue)
                    {
                        var cost = new Cost
                        { 
                            VehicleId = insurance.VehicleId.Value,
                            Type = CostType.Ubezpieczenie,
                            Opis = $"Ubezpieczenie: {insurance.PolicyNumber} ({insurance.InsurareName})",
                            Kwota = insurance.Cost,
                            Data = insurance.StartDate
                        };
                        Costs.Add(cost);

                    }


                }
            }
        }

       
    }
}