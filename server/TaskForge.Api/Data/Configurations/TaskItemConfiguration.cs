using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskForge.Api.Entities;

namespace TaskForge.Api.Data.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");

        builder.Property(t => t.Title).HasMaxLength(200);
        builder.Property(t => t.RowVersion).IsRowVersion();

        builder.HasIndex(t => new { t.ColumnId, t.Position });
        builder.HasIndex(t => new { t.ProjectId, t.Number }).IsUnique();
        builder.HasIndex(t => new { t.ProjectId, t.AssigneeId });
        builder.HasIndex(t => new { t.ProjectId, t.DueDate });
        builder.HasIndex(t => new { t.AssigneeId, t.CompletedAt });

        // Tasks are never removed by a cascade. SQL Server rejects multiple cascade paths
        // (Project -> Task and Project -> Board -> Column -> Task), and deleting a column
        // shouldn't silently wipe its tasks anyway. Services handle these deletes explicitly.
        builder.HasOne(t => t.Project)
            .WithMany()
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Column)
            .WithMany(c => c.Tasks)
            .HasForeignKey(t => t.ColumnId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Assignee)
            .WithMany()
            .HasForeignKey(t => t.AssigneeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Reporter)
            .WithMany()
            .HasForeignKey(t => t.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
