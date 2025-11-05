namespace ClientTradePortal.Trading.Api.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(t => t.TransactionId);

        builder.Property(t => t.Amount)
            .HasColumnType("decimal(15,2)")
            .IsRequired();

        builder.Property(t => t.BalanceBefore)
            .HasColumnType("decimal(15,2)")
            .IsRequired();

        builder.Property(t => t.BalanceAfter)
            .HasColumnType("decimal(15,2)")
            .IsRequired();

        builder.Property(t => t.TransactionType)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.HasIndex(t => new { t.AccountId, t.CreatedAt });
    }
}