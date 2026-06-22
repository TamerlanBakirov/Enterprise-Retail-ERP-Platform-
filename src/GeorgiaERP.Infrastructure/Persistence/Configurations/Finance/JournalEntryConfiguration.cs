using GeorgiaERP.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations.Finance;

public class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("journal_entries");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.EntryNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(j => j.EntryNumber)
            .IsUnique();

        builder.Property(j => j.EntryDate);

        builder.Property(j => j.Description)
            .HasMaxLength(500);

        builder.Property(j => j.SourceType)
            .HasMaxLength(50);

        builder.Property(j => j.SourceId);

        builder.Property(j => j.Status)
            .HasConversion<string>();

        builder.Property(j => j.TotalDebit)
            .HasPrecision(18, 2);

        builder.Property(j => j.TotalCredit)
            .HasPrecision(18, 2);

        builder.Property(j => j.PostedAt);

        builder.Property(j => j.PostedBy);

        builder.Property(j => j.ReversedById);

        builder.Property(j => j.CreatedAt);

        builder.Property(j => j.CreatedBy);

        builder.HasMany(j => j.Lines)
            .WithOne(l => l.JournalEntry)
            .HasForeignKey(l => l.JournalEntryId);

        // Date ordering for financial period queries
        builder.HasIndex(j => j.EntryDate)
            .HasDatabaseName("IX_journal_entries_entry_date");

        // Source document lookup (POS sale, purchase, etc.)
        builder.HasIndex(j => new { j.SourceType, j.SourceId })
            .HasDatabaseName("IX_journal_entries_source");

        // Status filter for posted/draft/reversed entries
        builder.HasIndex(j => j.Status)
            .HasDatabaseName("IX_journal_entries_status");

        // Date ordering for chronological listing
        builder.HasIndex(j => j.CreatedAt)
            .HasDatabaseName("IX_journal_entries_created_at");
    }
}
