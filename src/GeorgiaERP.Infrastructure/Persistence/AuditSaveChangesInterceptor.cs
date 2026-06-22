using System.Text.Json;
using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Compliance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GeorgiaERP.Infrastructure.Persistence;

public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IAuditContextAccessor _auditContext;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public AuditSaveChangesInterceptor(IAuditContextAccessor auditContext)
    {
        _auditContext = auditContext;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            AddAuditEntries(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
            AddAuditEntries(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    private void AddAuditEntries(DbContext context)
    {
        context.ChangeTracker.DetectChanges();

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(e => e.Entity.GetType() != typeof(AuditLog))
            .ToList();

        foreach (var entry in entries)
        {
            var entityType = entry.Entity.GetType().Name;
            var entityId = GetEntityId(entry);
            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Create,
                EntityState.Modified => AuditAction.Update,
                EntityState.Deleted => AuditAction.Delete,
                _ => throw new InvalidOperationException()
            };

            string? oldValues = null;
            string? newValues = null;
            string? changedProperties = null;

            var scalarProperties = entry.Properties
                .Where(p => !p.Metadata.IsShadowProperty())
                .Where(p => p.Metadata.ClrType.IsScalarType())
                .ToList();

            switch (entry.State)
            {
                case EntityState.Added:
                    newValues = SerializeValues(scalarProperties, p => p.CurrentValue);
                    break;

                case EntityState.Modified:
                    var modifiedProps = scalarProperties
                        .Where(p => p.IsModified)
                        .ToList();

                    if (modifiedProps.Count == 0)
                        continue;

                    oldValues = SerializeValues(modifiedProps, p => p.OriginalValue);
                    newValues = SerializeValues(modifiedProps, p => p.CurrentValue);
                    changedProperties = JsonSerializer.Serialize(
                        modifiedProps.Select(p => p.Metadata.Name).ToList(), JsonOptions);
                    break;

                case EntityState.Deleted:
                    oldValues = SerializeValues(scalarProperties, p => p.OriginalValue);
                    break;
            }

            var auditLog = AuditLog.Create(
                entityType,
                entityId,
                action,
                oldValues,
                newValues,
                changedProperties,
                _auditContext.UserId,
                _auditContext.IpAddress);

            context.Add(auditLog);
        }
    }

    private static string GetEntityId(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var idProperty = entry.Properties.FirstOrDefault(p =>
            string.Equals(p.Metadata.Name, "Id", StringComparison.OrdinalIgnoreCase));

        if (idProperty is not null)
            return idProperty.CurrentValue?.ToString() ?? string.Empty;

        var keyValues = entry.Properties
            .Where(p => p.Metadata.IsPrimaryKey())
            .Select(p => p.CurrentValue?.ToString() ?? string.Empty);

        return string.Join(",", keyValues);
    }

    private static string SerializeValues(
        IEnumerable<Microsoft.EntityFrameworkCore.ChangeTracking.PropertyEntry> properties,
        Func<Microsoft.EntityFrameworkCore.ChangeTracking.PropertyEntry, object?> valueSelector)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var prop in properties)
        {
            var value = valueSelector(prop);
            dict[prop.Metadata.Name] = value;
        }
        return JsonSerializer.Serialize(dict, JsonOptions);
    }
}

internal static class TypeExtensions
{
    public static bool IsScalarType(this Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return underlying.IsPrimitive
            || underlying.IsEnum
            || underlying == typeof(string)
            || underlying == typeof(decimal)
            || underlying == typeof(DateTime)
            || underlying == typeof(DateTimeOffset)
            || underlying == typeof(TimeSpan)
            || underlying == typeof(Guid)
            || underlying == typeof(byte[]);
    }
}
