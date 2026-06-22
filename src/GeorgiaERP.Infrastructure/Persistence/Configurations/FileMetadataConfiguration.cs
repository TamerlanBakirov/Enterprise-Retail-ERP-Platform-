using GeorgiaERP.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations;

public class FileMetadataConfiguration : IEntityTypeConfiguration<FileMetadata>
{
    public void Configure(EntityTypeBuilder<FileMetadata> builder)
    {
        builder.ToTable("file_metadata");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.FileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(f => f.StoredFileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasIndex(f => f.StoredFileName)
            .IsUnique();

        builder.Property(f => f.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(f => f.SizeBytes)
            .IsRequired();

        builder.Property(f => f.Category)
            .HasMaxLength(50);

        builder.Property(f => f.EntityType)
            .HasMaxLength(100);

        builder.HasIndex(f => new { f.EntityType, f.EntityId })
            .HasDatabaseName("IX_file_metadata_entity");

        builder.HasIndex(f => f.UploadedBy)
            .HasDatabaseName("IX_file_metadata_uploader");
    }
}
