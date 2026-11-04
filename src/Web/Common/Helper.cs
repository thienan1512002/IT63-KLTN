using System.Net;
using System.Text.RegularExpressions;

namespace Web.Common;

/// <summary>Base name for IStringLocalizerFactory (embeds: Resources/SharedResource*.resx) — no marker class required.</summary>
public static class Helper
{
    /// <summary>Resource logical name: {RootNamespace}.Resources.{BaseFileName} without culture.</summary>
    public const string SharedResource = "Web.Resources.SharedResource";

    /// <summary>Strips HTML tags and decodes HTML entities from a string.</summary>
    public static string StripHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        var stripped = Regex.Replace(html, "<[^>]*>", string.Empty);
        return WebUtility.HtmlDecode(stripped).Trim();
    }
}
