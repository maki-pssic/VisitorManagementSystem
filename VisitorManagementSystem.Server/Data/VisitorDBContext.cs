using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Server.Models;

namespace VisitorManagementSystem.Server.Data
{
    public class VisitorDBContext : DbContext
    {
        public VisitorDBContext(DbContextOptions<VisitorDBContext> options)
            : base(options)
        {
        }

        // Add the new sets for the relational structure
        public DbSet<Registration> Registrations { get; set; }
        public DbSet<Visitor> Visitors { get; set; }
        public DbSet<VehiclePass> VehiclePasses { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<Registrant> Registrants { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 1. Registration Table
            modelBuilder.Entity<Registration>(entity =>
            {
                entity.ToTable("Registrations");
                entity.HasKey(e => e.Id);
            });

            // 2. Visitor Table - FIX HERE
            modelBuilder.Entity<Visitor>(entity =>
            {
                entity.ToTable("Visitors");
                entity.HasOne(v => v.Registration) // Use the property v.Registration
                      .WithMany(r => r.Visitors)
                      .HasForeignKey(v => v.RegistrationId);
            });

            // 3. VehiclePass Table - FIX HERE
            modelBuilder.Entity<VehiclePass>(entity =>
            {
                entity.ToTable("VehiclePasses");
                entity.HasKey(e => e.Id);

                entity.HasOne(vp => vp.Registration) // Use the property vp.Registration
                      .WithMany(r => r.Vehicles)
                      .HasForeignKey(vp => vp.RegistrationId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // 4. Branch Table
            modelBuilder.Entity<Branch>(entity =>
            {
                entity.ToTable("Branches");
                entity.HasKey(e => e.Id);
            });

            // 4. Branch Table
            modelBuilder.Entity<Registrant>(entity =>
            {
                entity.ToTable("Registrants");
                entity.HasKey(e => e.Id);
            });
        }
    }
}