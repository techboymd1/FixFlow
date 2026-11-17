using FixFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Data;

public class FixFlowDbContext(DbContextOptions<FixFlowDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<RepairJob> RepairJobs => Set<RepairJob>();
    public DbSet<RepairEstimate> RepairEstimates => Set<RepairEstimate>();
    public DbSet<JobNote> JobNotes => Set<JobNote>();
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<JobPart> JobParts => Set<JobPart>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<JobStatusHistory> JobStatusHistories => Set<JobStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasIndex(user => user.UserName).IsUnique();
        });

        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.HasIndex(equipment => new { equipment.CustomerId, equipment.SerialNumber });
            entity.HasOne(equipment => equipment.Customer)
                .WithMany(customer => customer.Equipment)
                .HasForeignKey(equipment => equipment.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RepairJob>(entity =>
        {
            entity.HasIndex(job => job.JobReference).IsUnique();
            entity.HasIndex(job => new { job.Status, job.ReceiveDate });
            entity.Property(job => job.Status).HasConversion<string>().HasMaxLength(30);
            entity.HasOne(job => job.Customer)
                .WithMany()
                .HasForeignKey(job => job.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(job => job.Equipment)
                .WithMany(equipment => equipment.RepairJobs)
                .HasForeignKey(job => job.EquipmentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(job => job.Technician)
                .WithMany(user => user.AssignedJobs)
                .HasForeignKey(job => job.TechnicianId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RepairEstimate>(entity =>
        {
            entity.Property(estimate => estimate.PartsAmount).HasPrecision(18, 2);
            entity.Property(estimate => estimate.LabourAmount).HasPrecision(18, 2);
            entity.Property(estimate => estimate.ApprovalStatus).HasConversion<string>().HasMaxLength(20);
            entity.HasOne(estimate => estimate.RepairJob)
                .WithOne(job => job.Estimate)
                .HasForeignKey<RepairEstimate>(estimate => estimate.RepairJobId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<JobNote>(entity =>
        {
            entity.HasOne(note => note.RepairJob).WithMany(job => job.Notes).HasForeignKey(note => note.RepairJobId);
            entity.HasOne(note => note.CreatedByUser).WithMany().HasForeignKey(note => note.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Part>(entity =>
        {
            entity.HasIndex(part => part.Sku).IsUnique();
            entity.Property(part => part.CostPrice).HasPrecision(18, 2);
            entity.Property(part => part.SellingPrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<JobPart>(entity =>
        {
            entity.Property(jobPart => jobPart.UnitPrice).HasPrecision(18, 2);
            entity.HasIndex(jobPart => new { jobPart.RepairJobId, jobPart.PartId });
            entity.HasOne(jobPart => jobPart.RepairJob).WithMany(job => job.PartsUsed).HasForeignKey(jobPart => jobPart.RepairJobId);
            entity.HasOne(jobPart => jobPart.Part).WithMany(part => part.JobParts).HasForeignKey(jobPart => jobPart.PartId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.HasIndex(movement => movement.RepairJobId);
            entity.HasIndex(movement => movement.CreatedByUserId);

            entity.HasOne(movement => movement.Part)
                .WithMany(part => part.StockMovements)
                .HasForeignKey(movement => movement.PartId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(movement => movement.RepairJob)
                .WithMany()
                .HasForeignKey(movement => movement.RepairJobId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(movement => movement.CreatedByUser)
                .WithMany()
                .HasForeignKey(movement => movement.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasIndex(payment => payment.ReceiptNumber).IsUnique();
            entity.Property(payment => payment.Amount).HasPrecision(18, 2);
            entity.HasOne(payment => payment.RepairJob).WithMany(job => job.Payments).HasForeignKey(payment => payment.RepairJobId);
            entity.HasOne(payment => payment.ReceivedByUser).WithMany().HasForeignKey(payment => payment.ReceivedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<JobStatusHistory>(entity =>
        {
            entity.Property(history => history.FromStatus).HasConversion<string>().HasMaxLength(30);
            entity.Property(history => history.ToStatus).HasConversion<string>().HasMaxLength(30);
            entity.HasOne(history => history.RepairJob).WithMany(job => job.StatusHistory).HasForeignKey(history => history.RepairJobId);
            entity.HasOne(history => history.ChangedByUser).WithMany().HasForeignKey(history => history.ChangedByUserId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}