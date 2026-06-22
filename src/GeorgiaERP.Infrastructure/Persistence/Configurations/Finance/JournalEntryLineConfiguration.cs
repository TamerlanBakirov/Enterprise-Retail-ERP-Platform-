using GeorgiaERP.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Finance;

public class JournalEntryLineConfiguration : IEntityTypeConfiguration<JournalEntryLine>
{
    public void Configure(EntityTypeBuilder<JournalEntryLine> builder)
    {
        builder.ToTable("journal_entry_lines");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.JournalEntryId);

        builder.Property(l => l.LineNumber);

        builder.Property(l => l.AccountId);

        builder.Property(l => l.Description)
            .HasMaxLength(500);

        builder.Property(l => l.DebitAmount)
            .HasPrecision(18, 2);

        builder.Property(l => l.CreditAmount)
            .HasPrecision(18, 2);

        builder.HasOne(l => l.JournalEntry)
            .WithMany(j => j.Lines)
            .HasForeignKey(l => l.JournalEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Account)
            .WithMany()
            .HasForeignKey(l => l.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK index for journal entry line lookups
        builder.HasIndex(l => l.JournalEntryId)
            .HasDatabaseName("IX_journal_entry_lines_journal_entry");

        // Account balance queries - sum debits/credits by account
        builder.HasIndex(l => l.AccountId)
            .HasDatabaseName("IX_journal_entry_lines_account");
    }
}
