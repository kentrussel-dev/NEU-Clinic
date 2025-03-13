using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.Data
{
    public class AppDbContext : IdentityDbContext<Users>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Existing tables
        public DbSet<PersonalDetails> PersonalDetails { get; set; }
        public DbSet<HealthDetails> HealthDetails { get; set; }

        // New table for submitted health details
        public DbSet<SubmittedHealthDetails> SubmittedHealthDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Call this once at the beginning

            // Configure the one-to-one relationship between Users and PersonalDetails
            modelBuilder.Entity<Users>()
                .HasOne(u => u.PersonalDetails)
                .WithOne(p => p.User)
                .HasForeignKey<PersonalDetails>(p => p.UserId) // Foreign key in PersonalDetails
                .OnDelete(DeleteBehavior.Cascade);

            // Configure the one-to-one relationship between Users and HealthDetails
            modelBuilder.Entity<Users>()
                .HasOne(u => u.HealthDetails)
                .WithOne(h => h.User)
                .HasForeignKey<HealthDetails>(h => h.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure the one-to-many relationship between Users and SubmittedHealthDetails
            modelBuilder.Entity<SubmittedHealthDetails>()
                .HasOne(s => s.User)
                .WithMany(u => u.SubmittedHealthDetails)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}