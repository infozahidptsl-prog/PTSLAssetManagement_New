using AssetManagement.Core.Entities;
using AssetManagement.Core.Enums;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Device> Devices { get; set; } = null!;
    public DbSet<Employee> Employees { get; set; } = null!;
    public DbSet<Handover> Handovers { get; set; } = null!;
    public DbSet<Brand> Brands { get; set; } = null!;
    public DbSet<DeviceCategoryModel> DeviceCategories { get; set; } = null!;
    public DbSet<Designation> Designations { get; set; } = null!;
    public DbSet<Department> Departments { get; set; } = null!;
    public DbSet<RepairRecord> Repairs { get; set; } = null!;
    public DbSet<Requisition> Requisitions { get; set; } = null!;
    public DbSet<Purchase> Purchases { get; set; } = null!;
    public DbSet<PurchaseItem> PurchaseItems { get; set; } = null!;
    public DbSet<ProjectManager> ProjectManagers { get; set; } = null!;
    public DbSet<OwnerCompany> OwnerCompanies { get; set; } = null!;
    public DbSet<Team> Teams { get; set; } = null!;
    public DbSet<Location> Locations { get; set; } = null!;
    public DbSet<BloodGroup> BloodGroups { get; set; } = null!;
    public DbSet<AutomationFinding> AutomationFindings { get; set; } = null!;
    public DbSet<AutomationSettings> AutomationSettings { get; set; } = null!;
    public DbSet<PurchaseItemEvidence> PurchaseItemEvidences { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Setup relationships and constraints here if needed.
        modelBuilder.Entity<Handover>()
            .HasOne(h => h.Device)
            .WithMany()
            .HasForeignKey(h => h.DeviceId);
            
        modelBuilder.Entity<RepairRecord>()
            .HasOne(r => r.Device)
            .WithMany()
            .HasForeignKey(r => r.DeviceId);
            
        modelBuilder.Entity<Device>()
            .HasOne(d => d.ParentDevice)
            .WithMany()
            .HasForeignKey(d => d.ParentDeviceId)
            .OnDelete(DeleteBehavior.ClientSetNull);
            
        // For Employee DeviceIds, since it's a List<string> not mapped to a complex type
        modelBuilder.Entity<Employee>().Ignore(e => e.DeviceIds);
        modelBuilder.Entity<Employee>().Ignore(e => e.ActiveCount);
        modelBuilder.Entity<Purchase>()
            .HasMany(p => p.Items)
            .WithOne(i => i.Purchase)
            .HasForeignKey(i => i.PurchaseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PurchaseItem>()
            .HasMany(i => i.Devices)
            .WithOne(d => d.PurchaseItem)
            .HasForeignKey(d => d.PurchaseItemId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        modelBuilder.Entity<PurchaseItem>()
            .HasMany(i => i.Evidences)
            .WithOne(e => e.PurchaseItem)
            .HasForeignKey(e => e.PurchaseItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

