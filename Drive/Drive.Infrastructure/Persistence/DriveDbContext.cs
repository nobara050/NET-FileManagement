using Drive.Domain.Entities;
using Drive.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace Drive.Infrastructure.Persistence
{
    public class DriveDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public DriveDbContext(DbContextOptions<DriveDbContext> options) : base(options)
        {
        }

        public DbSet<DriveItem> DriveItems => Set<DriveItem>();

        public DbSet<DriveItemRoleAssignment> DriveItemRoleAssignments
            => Set<DriveItemRoleAssignment>();

        // Use this method to use IEntityTypeConfiguration<T> classes to configure the model
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(DriveDbContext).Assembly);
        }
    }
}
