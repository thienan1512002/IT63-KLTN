using System.Globalization;
using System.Resources;
using Web.Common;

namespace Web.Services.Localization;

/// <summary>
/// Resolves UI strings from embedded .resx via <see cref="ResourceManager"/>.
/// Using <c>ResourceManager</c> directly matches <c>Web.Resources.SharedResource.resources</c> in the main
/// and satellite folders (e.g. vi-VN) reliably; <see cref="IStringLocalizerFactory.Create"/>(name, location)
/// can fail in some hosts when the wrong assembly is resolved.
/// </summary>
public class LanguageService
{
    // Base name must match manifest: Web.Resources.SharedResource (see build output for *.resources)
    private static readonly ResourceManager Rm = new(Helper.SharedResource, typeof(LanguageService).Assembly);

    /// <summary>Localized text for <paramref name="key"/>, with optional <c>string.Format</c> placeholders.</summary>
    public string GetKey(string key, params object[] args)
    {
        var culture = CultureInfo.CurrentUICulture;
        // Walks parent cultures and neutral resources; then invariant if needed
        var s = Rm.GetString(key, culture);
        if (s is null)
            s = Rm.GetString(key, CultureInfo.InvariantCulture);
        if (s is null)
            s = key;
        if (args is { Length: > 0 })
            return string.Format(culture, s, args);
        return s;
    }

    public static string TranslateByCulture(string key, CultureInfo culture, params object[] args)
    {
        // Walks parent cultures and neutral resources; then invariant if needed
        var s = Rm.GetString(key, culture);
        if (s is null)
            s = Rm.GetString(key, CultureInfo.InvariantCulture);
        if (s is null)
            s = key;
        if (args is { Length: > 0 })
            return string.Format(culture, s, args);
        return s;
    }

    public static string Translate(string key, params object[] args)
    {
        var culture = CultureInfo.CurrentUICulture;
        // Walks parent cultures and neutral resources; then invariant if needed
        var s = Rm.GetString(key, culture);
        if (s is null)
            s = Rm.GetString(key, CultureInfo.InvariantCulture);
        if (s is null)
            s = key;
        if (args is { Length: > 0 })
            return string.Format(culture, s, args);
        return s;
    }


}
