using System.Globalization;
using System.Resources;
using GeorgiaERP.Application.Common;
using Microsoft.Extensions.Logging;

namespace GeorgiaERP.Infrastructure.Localization;

/// <summary>
/// Localization service backed by .resx resource files in GeorgiaERP.Application.
/// Supports en-US (default) and ka-GE cultures.
/// Thread-safe: ResourceManager handles concurrency internally.
/// </summary>
public sealed class ResourceLocalizationService : ILocalizationService
{
    private readonly ResourceManager _resourceManager;
    private readonly ILogger<ResourceLocalizationService> _logger;

    /// <summary>
    /// Supported cultures for the ERP platform.
    /// </summary>
    public static readonly CultureInfo Georgian = new("ka-GE");
    public static readonly CultureInfo English = new("en-US");

    public ResourceLocalizationService(ILogger<ResourceLocalizationService> logger)
    {
        _logger = logger;
        _resourceManager = new ResourceManager(
            "GeorgiaERP.Application.Resources.Messages",
            typeof(ILocalizationService).Assembly);
    }

    public string Get(string key)
    {
        return Get(key, CultureInfo.CurrentUICulture);
    }

    public string Get(string key, CultureInfo culture)
    {
        try
        {
            var value = _resourceManager.GetString(key, culture);
            if (value is not null)
                return value;

            _logger.LogDebug("Localization key not found: {Key} for culture {Culture}", key, culture.Name);
            return key; // Fallback: return the key itself
        }
        catch (MissingManifestResourceException ex)
        {
            _logger.LogWarning(ex, "Resource file not found for culture {Culture}", culture.Name);
            return key;
        }
    }

    public string GetFormatted(string key, params object[] args)
    {
        return GetFormatted(key, CultureInfo.CurrentUICulture, args);
    }

    public string GetFormatted(string key, CultureInfo culture, params object[] args)
    {
        var template = Get(key, culture);
        try
        {
            return string.Format(culture, template, args);
        }
        catch (FormatException)
        {
            _logger.LogWarning("Format error for key {Key} with {ArgCount} arguments", key, args.Length);
            return template;
        }
    }

    public bool HasKey(string key)
    {
        return _resourceManager.GetString(key, CultureInfo.InvariantCulture) is not null;
    }
}
