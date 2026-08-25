using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class DemoOrderConfiguration : IEntityTypeConfiguration<DemoOrder>
{
    public void Configure(EntityTypeBuilder<DemoOrder> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.ConnectStoneOrderId).HasMaxLength(64);
        builder.Property(o => o.Description).IsRequired().HasMaxLength(200);
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(o => o.ConnectStoneOrderId).IsUnique();
        builder.HasIndex(o => o.Status);
    }
}
