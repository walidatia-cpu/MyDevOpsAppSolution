using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyDevOpsApp.Data.Entities;
using MyDevOpsApp.Models;

namespace MyDevOpsApp.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<PayrollBatch> PayrollBatches => Set<PayrollBatch>();
    public DbSet<PayrollRecord> PayrollRecords => Set<PayrollRecord>();
    public DbSet<PayrollItem> PayrollItems => Set<PayrollItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // الفهارس والعلاقات
        builder.Entity<Employee>()
            .HasIndex(e => e.NationalId);

        builder.Entity<Employee>()
            .HasIndex(e => e.PaymentNumber);

        builder.Entity<PayrollBatch>()
            .HasIndex(b => new { b.Year, b.Month, b.PayrollType });

        builder.Entity<PayrollRecord>()
            .HasIndex(r => new { r.PayrollBatchId, r.EmployeeId })
            .IsUnique();

        builder.Entity<PayrollRecord>()
            .HasOne(r => r.PayrollBatch)
            .WithMany(b => b.Records)
            .HasForeignKey(r => r.PayrollBatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PayrollRecord>()
            .HasOne(r => r.Employee)
            .WithMany(e => e.PayrollRecords)
            .HasForeignKey(r => r.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PayrollItem>()
            .HasOne(i => i.PayrollRecord)
            .WithMany(r => r.Items)
            .HasForeignKey(i => i.PayrollRecordId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
