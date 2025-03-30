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
        public DbSet<RoomAppointment> RoomAppointments { get; set; }
        public DbSet<RoomAppointmentUser> RoomAppointmentUsers { get; set; }
        public DbSet<Notification> Notifications { get; set; }


        // New table for submitted health details
        public DbSet<SubmittedHealthDetails> SubmittedHealthDetails { get; set; }
        public DbSet<PersonalAppointment> PersonalAppointments { get; set; }

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

            foreach (var property in typeof(SubmittedHealthDetails).GetProperties()
                .Where(p => p.Name.EndsWith("Status") && p.PropertyType == typeof(string)))
            {
                modelBuilder.Entity<SubmittedHealthDetails>()
                    .Property(property.Name)
                    .HasDefaultValue("Pending");
            }

            // Configure the many-to-many relationship
            modelBuilder.Entity<RoomAppointmentUser>()
                .HasKey(rau => new { rau.RoomAppointmentId, rau.UserId });

            modelBuilder.Entity<RoomAppointmentUser>()
                .HasOne(rau => rau.RoomAppointment)
                .WithMany(ra => ra.RoomAppointmentUsers)
                .HasForeignKey(rau => rau.RoomAppointmentId);

            modelBuilder.Entity<RoomAppointmentUser>()
                .HasOne(rau => rau.User)
                .WithMany(u => u.RoomAppointmentUsers)
                .HasForeignKey(rau => rau.UserId);

            modelBuilder.Entity<Notification>()
               .HasOne(n => n.User)
               .WithMany(u => u.Notifications)
               .HasForeignKey(n => n.UserId);

            modelBuilder.Entity<PersonalAppointment>()
               .HasOne(a => a.User)
               .WithMany()
               .HasForeignKey(a => a.UserId);
        }
    }
}