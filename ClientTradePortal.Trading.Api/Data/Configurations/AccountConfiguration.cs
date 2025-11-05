namespace ClientTradePortal.Trading.Api.Data.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.HasKey(a => a.AccountId);

        builder.Property(a => a.CashBalance)
            .HasColumnType("decimal(15,2)")
            .IsRequired();

        builder.Property(a => a.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .IsRequired();

        builder.HasIndex(a => a.ClientId);

        builder.HasMany(a => a.Positions)
            .WithOne(p => p.Account)
            .HasForeignKey(p => p.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Orders)
            .WithOne(o => o.Account)
            .HasForeignKey(o => o.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Transactions)
            .WithOne(t => t.Account)
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}