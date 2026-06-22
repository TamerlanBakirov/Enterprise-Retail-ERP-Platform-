using GeorgiaERP.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeorgiaERP.Infrastructure.Persistence.Configurations;

public class WebhookSubscriptionConfiguration : IEntityTypeConfiguration<WebhookSubscription>
{
    public void Configure(EntityTypeBuilder<WebhookSubscription> builder)
    {
        builder.ToTable("webhook_subscriptions");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(w => w.Url)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(w => w.Secret)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(w => w.EventTypes)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(w => w.IsActive)
            .HasDefaultValue(true);

        builder.Property(w => w.MaxRetries)
            .HasDefaultValue(3);

        builder.Property(w => w.LastDeliveryStatus)
            .HasMaxLength(500);

        builder.HasIndex(w => w.IsActive)
            .HasDatabaseName("IX_webhook_subscriptions_active");
    }
}

public class WebhookDeliveryLogConfiguration : IEntityTypeConfiguration<WebhookDeliveryLog>
{
    public void Configure(EntityTypeBuilder<WebhookDeliveryLog> builder)
    {
        builder.ToTable("webhook_delivery_logs");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.EventType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.Payload)
            .IsRequired();

        builder.Property(d => d.ResponseBody)
            .HasMaxLength(2000);

        builder.Property(d => d.ErrorMessage)
            .HasMaxLength(2000);

        builder.HasIndex(d => d.SubscriptionId)
            .HasDatabaseName("IX_webhook_delivery_logs_subscription");

        builder.HasIndex(d => d.AttemptedAt)
            .HasDatabaseName("IX_webhook_delivery_logs_attempted_at");
    }
}
