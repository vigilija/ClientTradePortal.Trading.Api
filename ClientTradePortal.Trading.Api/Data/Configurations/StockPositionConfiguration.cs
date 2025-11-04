using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ClientTradePortal.Trading.Api.Entities;

namespace ClientTradePortal.Trading.Api.Data.Configurations;

public class StockPositionConfiguration : IEntityTypeConfiguration<StockPosition>
{
    public void Configure(EntityTypeBuilder<StockPosition> builder)
    {
        builder.HasKey(p => p.PositionId);

        builder.Property(p => p.Symbol)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(p => p.Quantity)
            .IsRequired();

        builder.Property(p => p.AveragePrice)
            .HasColumnType("decimal(15,4)")
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .IsRequired();

        builder.HasIndex(p => new { p.AccountId, p.Symbol })
            .IsUnique();
    }
}