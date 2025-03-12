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
            base.OnModelCreating(modelBuilder);

            // Configure the PersonalDetails entity
            modelBuilder.Entity<PersonalDetails>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure the HealthDetails entity
            modelBuilder.Entity<HealthDetails>()
                .HasOne(h => h.User)
                .WithMany()
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure the SubmittedHealthDetails entity
            modelBuilder.Entity<SubmittedHealthDetails>()
                .HasOne(s => s.User)
                .WithMany(u => u.SubmittedHealthDetails)
                .HasForeignKey(s => s.UserId);

            base.OnModelCreating(modelBuilder);

            // Ignore the User property during validation (if needed)
            modelBuilder.Entity<PersonalDetails>()
                .Ignore(p => p.User);

            modelBuilder.Entity<HealthDetails>()
                .Ignore(h => h.User);

            modelBuilder.Entity<SubmittedHealthDetails>()
                .Ignore(shd => shd.User);
        }
    }
}