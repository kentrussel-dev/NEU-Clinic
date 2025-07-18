﻿using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
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
        public DbSet<SubmittedHealthDetails> SubmittedHealthDetails { get; set; }
        public DbSet<PersonalAppointment> PersonalAppointments { get; set; }
        public DbSet<PersonalMessage> PersonalMessages { get; set; }

        // Add SystemConfiguration table
        public DbSet<SystemConfiguration> SystemConfigurations { get; set; }
        public DbSet<Archive> Archives { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Call this once at the beginning

            // Configure the one-to-one relationship between Users and PersonalDetails
            modelBuilder.Entity<Users>()
                .HasOne(u => u.PersonalDetails)
                .WithOne(p => p.User)
                .HasForeignKey<PersonalDetails>(p => p.UserId)
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

            // Configure default values for status fields
            foreach (var property in typeof(SubmittedHealthDetails).GetProperties()
                .Where(p => p.Name.EndsWith("Status") && p.PropertyType == typeof(string)))
            {
                modelBuilder.Entity<SubmittedHealthDetails>()
                    .Property(property.Name)
                    .HasDefaultValue("Pending");
            }

            // Configure the many-to-many relationship for room appointments
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

            // Configure notifications
            modelBuilder.Entity<Notification>()
               .HasOne(n => n.User)
               .WithMany(u => u.Notifications)
               .HasForeignKey(n => n.UserId);

            // Configure personal appointments
            modelBuilder.Entity<PersonalAppointment>()
               .HasOne(a => a.User)
               .WithMany()
               .HasForeignKey(a => a.UserId);

            // Configure personal messages
            modelBuilder.Entity<PersonalMessage>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PersonalMessage>()
                .HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed initial system configuration
            var currentYear = DateTime.Now.Year;
            var defaultExpiry = new DateTime(currentYear, 7, 25);
            if (DateTime.Now > defaultExpiry)
            {
                defaultExpiry = new DateTime(currentYear + 1, 7, 25);
            }

            modelBuilder.Entity<SystemConfiguration>().HasData(
                new SystemConfiguration
                {
                    Id = 1,
                    HealthDocumentsExpiryDate = defaultExpiry,
                    LastUpdated = DateTime.UtcNow
                }
            );
        }
    }
}