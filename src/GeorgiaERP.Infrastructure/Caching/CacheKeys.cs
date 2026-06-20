namespace GeorgiaERP.Infrastructure.Caching;

/// <summary>
/// Centralized cache key patterns for Redis. Using constants prevents key
/// collisions and makes cache invalidation easier to audit.
/// </summary>
public static class CacheKeys
{
    // Product catalog
    public const string ProductList = "products:list";
    public const string ProductById = "products:id:";
    public const string Categories = "products:categories";

    // Pricing
    public const string PriceLists = "pricing:lists";
    public const string PriceListItems = "pricing:items:";
    public const string Promotions = "pricing:promotions";

    // User permissions (checked on every request by the permission filter)
    public const string UserPermissions = "user:permissions:";

    // RS.GE external API results
    public const string RsGeTinName = "rsge:tin:name:";
    public const string RsGeTinVat = "rsge:tin:vat:";
    public const string RsGeUnits = "rsge:units";
    public const string RsGeTransportTypes = "rsge:transport-types";
    public const string RsGeWaybillTypes = "rsge:waybill-types";

    // Organization (rarely changes)
    public const string Stores = "org:stores";
    public const string Warehouses = "org:warehouses";

    /// <summary>Default TTL for frequently-changing data (product lists, pricing).</summary>
    public static readonly TimeSpan ShortTtl = TimeSpan.FromMinutes(2);

    /// <summary>Default TTL for semi-static data (categories, org structure).</summary>
    public static readonly TimeSpan MediumTtl = TimeSpan.FromMinutes(10);

    /// <summary>Default TTL for rarely-changing external data (RS.GE reference data).</summary>
    public static readonly TimeSpan LongTtl = TimeSpan.FromHours(1);

    /// <summary>TTL for TIN validation results from RS.GE (external API call, expensive).</summary>
    public static readonly TimeSpan TinValidationTtl = TimeSpan.FromHours(24);

    /// <summary>TTL for user permission data (balance between security and performance).</summary>
    public static readonly TimeSpan PermissionsTtl = TimeSpan.FromMinutes(5);
}
