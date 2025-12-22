using EmployeeAttendanceSystem.Server.Domain;
using EmployeeAttendanceSystem.Server.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EmployeeAttendanceSystem.Server.Context
{
    public class ApplicationDbContext : IdentityDbContext<Employee>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<AttendanceLog> AttendanceLogs { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<AuditLog>(entity =>
            {
                entity.HasIndex(e => e.Timestamp).HasDatabaseName("IX_AuditLog_Timestamp");
                entity.HasIndex(e => e.UserId).HasDatabaseName("IX_AuditLog_UserId");
                entity.HasIndex(e => e.Action).HasDatabaseName("IX_AuditLog_Action");
                entity.HasIndex(e => new { e.EntityType, e.EntityId }).HasDatabaseName("IX_AuditLog_EntityType_EntityId");
            });

            builder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = "1", Name = "Admin", NormalizedName = "ADMIN" },
                new IdentityRole { Id = "2", Name = "Employee", NormalizedName = "EMPLOYEE" }
            );

            var adminUserId = "a1b2c3d4-e5f6-7890-abcd-ef1234567890";
            var passwordHasher = new PasswordHasher<Employee>();

            var adminUser = new Employee
            {
                Id = adminUserId,
                Name = "Admin",
                Surname = "User",
                Email = "admin@sirket.com",
                NormalizedEmail = "ADMIN@SIRKET.COM",
                UserName = "admin@sirket.com",
                NormalizedUserName = "ADMIN@SIRKET.COM",
                EmailConfirmed = true,
            };


            adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "3eb3fe66b31e3b4d10fa70b5cad49c7112294af6ae4e476a1c405155d45aa121");

            builder.Entity<Employee>().HasData(adminUser);

            builder.Entity<IdentityUserRole<string>>().HasData(
                new IdentityUserRole<string>
                {
                    RoleId = "1",
                    UserId = adminUserId
                }
            );

            builder.Entity<Employee>()
                .HasMany(u => u.Logs)
                .WithOne(l => l.Employee)
                .HasForeignKey(l => l.EmployeeID);
        }
    }
}