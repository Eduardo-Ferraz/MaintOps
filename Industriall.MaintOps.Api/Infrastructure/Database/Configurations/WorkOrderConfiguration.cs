using Industriall.MaintOps.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Industriall.MaintOps.Api.Infrastructure.Database.Configurations;

internal sealed class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrder> builder)
    {
        builder.ToTable("work_orders");

        builder.HasKey(wo => wo.Id);

        builder.Property(wo => wo.Id)
            .ValueGeneratedNever();

        builder.Property(wo => wo.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(wo => wo.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // MaintenanceSchedule is a Value Object – stored as owned/embedded columns.
        builder.OwnsOne(wo => wo.Schedule, schedule =>
        {
            schedule.Property(s => s.StartDate)
                .HasColumnName("schedule_start_date")
                .HasColumnType("timestamp with time zone");

            schedule.Property(s => s.EndDate)
                .HasColumnName("schedule_end_date")
                .HasColumnType("timestamp with time zone");
        });

        // Relationship: many WorkOrders → one Equipment (no cascade delete).
        builder.HasOne<Equipment>()
            .WithMany()
            .HasForeignKey(wo => wo.EquipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(wo => wo.EquipmentId);
        builder.HasIndex(wo => wo.Status);
    }
}
