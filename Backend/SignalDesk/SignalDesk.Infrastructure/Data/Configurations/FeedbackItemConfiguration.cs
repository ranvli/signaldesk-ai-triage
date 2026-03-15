using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SignalDesk.Domain.Entities;

namespace SignalDesk.Infrastructure.Data.Configurations;

public class FeedbackItemConfiguration : IEntityTypeConfiguration<FeedbackItem>
{
    public void Configure(EntityTypeBuilder<FeedbackItem> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Text)
            .IsRequired();

        builder.Property(f => f.Summary)
            .IsRequired();

        builder.Property(f => f.Category)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(f => f.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(f => f.Priority)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(f => f.CreatedAt)
            .IsRequired();
    }
}
