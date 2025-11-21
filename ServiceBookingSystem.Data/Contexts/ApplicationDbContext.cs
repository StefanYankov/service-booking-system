using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ServiceBookingSystem.Data.Entities.Common;
using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Data.Entities.Identity;

namespace ServiceBookingSystem.Data.Contexts;

/// <summary>
/// Represents the database context for the application, serving as the main gateway for database operations.
/// It integrates ASP.NET Core Identity and handles custom application entities.
/// It also automatically applies audit information and soft-delete logic.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    // A private static method used via reflection to construct the lambda for the global query filter.
    private static readonly MethodInfo SetIsDeletedQueryFilterMethod =
        typeof(ApplicationDbContext).GetMethod(
            nameof(SetIsDeletedQueryFilter),
            BindingFlags.NonPublic | BindingFlags.Static)!;
    
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // --- Domain Entities ---
    public DbSet<Category> Categories { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<OperatingHour> OperatingHours { get; set; }
    public DbSet<ServiceImage> ServiceImages { get; set; }
    public DbSet<Review> Reviews { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Category>()
            .HasIndex(c => c.Name)
            .IsUnique();
        
        // --- Manual Relationship Configuration to Prevent Cascade Cycles ---
 
        builder.Entity<Service>()
            .HasOne(s => s.Provider)
            .WithMany() // Using an empty WithMany specifies a one-way navigation
            .HasForeignKey(s => s.ProviderId)
            .OnDelete(DeleteBehavior.Restrict); // Prevents cascade delete

        builder.Entity<Review>()
            .HasOne(r => r.Customer)
            .WithMany() // One-way navigation
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Restrict); // Prevents cascade delete

        // --- Global Query Filter for Soft Deletes ---
        var deletableEntityTypes = builder.Model.GetEntityTypes()
            .Where(et => typeof(IDeletableEntity).IsAssignableFrom(et.ClrType));

        foreach (var deletableEntityType in deletableEntityTypes)
        {
            var method = SetIsDeletedQueryFilterMethod.MakeGenericMethod(deletableEntityType.ClrType);
            method.Invoke(null, new object[] { builder });
        }
        
        // --- Cascade Soft-Delete Behavior for Required Children ---
        builder.Entity<OperatingHour>()
            .HasQueryFilter(oh => !oh.Service.IsDeleted);

        builder.Entity<ServiceImage>()
            .HasQueryFilter(si => !si.Service.IsDeleted);
    }

    /// <summary>
    /// Saves all changes made in this context to the database.
    /// This override automatically applies audit and soft-delete rules before saving.
    /// </summary>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        this.ApplyAuditAndSoftDeleteRules();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <summary>
    /// Asynchronously saves all changes made in this context to the database.
    /// This override automatically applies audit and soft-delete rules before saving.
    /// </summary>
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        this.ApplyAuditAndSoftDeleteRules();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Applies rules for audit information (CreatedOn, ModifiedOn) and soft deletion
    /// to all tracked entities before they are saved to the database.
    /// </summary>
    private void ApplyAuditAndSoftDeleteRules()
    {
        // --- Soft Delete Logic ---
        var deletableEntries = this.ChangeTracker
            .Entries<IDeletableEntity>()
            .Where(e => e.State == EntityState.Deleted);

        foreach (var entry in deletableEntries)
        {
            var entity = entry.Entity;
            entity.IsDeleted = true;
            entity.DeletedOn = DateTime.UtcNow;
            entry.State = EntityState.Modified;
        }

        // --- Audit Info Logic ---
        var auditEntries = this.ChangeTracker
            .Entries<IAuditInfo>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in auditEntries)
        {
            var entity = entry.Entity;
            if (entry.State == EntityState.Added && entity.CreatedOn == default)
            {
                entity.CreatedOn = DateTime.UtcNow;
            }
            else
            {
                entity.ModifiedOn = DateTime.UtcNow;
            }
        }
    }
    
    /// <summary>
    /// A generic helper method to create a correctly-typed lambda expression for the global query filter.
    /// </summary>
    private static void SetIsDeletedQueryFilter<T>(ModelBuilder builder)
        where T : class, IDeletableEntity
    {
        builder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
    }
}
