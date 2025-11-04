using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ClientTradePortal.Trading.Api.Entities;

namespace ClientTradePortal.Trading.Api.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.OrderId);

        builder.Property(o => o.Symbol)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(o => o.OrderType)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(o => o.Quantity)
            .IsRequired();

        builder.Property(o => o.PricePerShare)
            .HasColumnType("decimal(15,4)")
            .IsRequired();

        builder.Property(o => o.TotalAmount)
            .HasColumnType("decimal(15,2)")
            .IsRequired();

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(o => o.ExchangeOrderId)
            .HasMaxLength(100);

        builder.Property(o => o.CreatedAt)
            .IsRequired();

        builder.Property(o => o.ErrorMessage)
            .HasMaxLength(500);

        builder.HasIndex(o => o.IdempotencyKey)
            .IsUnique();

        builder.HasIndex(o => new { o.AccountId, o.CreatedAt });
    }
}