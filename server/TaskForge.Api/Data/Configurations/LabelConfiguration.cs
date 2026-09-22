using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskForge.Api.Entities;

namespace TaskForge.Api.Data.Configurations;

public class LabelConfiguration : IEntityTypeConfiguration<Label>
{
    public void Configure(EntityTypeBuilder<Label> builder)
    {
        builder.Property(l => l.Name).HasMaxLength(30);
        builder.Property(l => l.Color).HasMaxLength(7);

        builder.HasIndex(l => new { l.ProjectId, l.Name }).IsUnique();

        builder.HasOne(l => l.Project)
            .WithMany(p => p.Labels)
            .HasForeignKey(l => l.ProjectId);
    }
}

public class TaskLabelConfiguration : IEntityTypeConfiguration<TaskLabel>
{
    public void Configure(EntityTypeBuilder<TaskLabel> builder)
    {
        builder.HasKey(tl => new { tl.TaskId, tl.LabelId });

        builder.HasOne(tl => tl.Task)
            .WithMany(t => t.Labels)
            .HasForeignKey(tl => tl.TaskId);

        builder.HasOne(tl => tl.Label)
            .WithMany(l => l.Tasks)
            .HasForeignKey(tl => tl.LabelId);
    }
}
