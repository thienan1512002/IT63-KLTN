using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Web.Services.Localization;

internal static class ResxFileHelper
{
    public static Dictionary<string, string> ReadResxData(string path)
    {
        if (!File.Exists(path)) return new Dictionary<string, string>(StringComparer.Ordinal);
        return ReadData(XDocument.Load(path, LoadOptions.PreserveWhitespace));
    }

    public static XDocument LoadOrCreate(string path)
    {
        if (File.Exists(path)) return XDocument.Load(path, LoadOptions.PreserveWhitespace);
        var root = new XElement("root",
            new XElement("resheader",
                new XAttribute("name", "resmimetype"),
                new XElement("value", "text/microsoft-resx")),
            new XElement("resheader",
                new XAttribute("name", "version"),
                new XElement("value", "2.0")),
            new XElement("resheader",
                new XAttribute("name", "reader"),
                new XElement("value", "System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")),
            new XElement("resheader",
                new XAttribute("name", "writer"),
                new XElement("value", "System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"))
        );
        return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root);
    }

    private static Dictionary<string, string> ReadData(XDocument doc)
    {
        var d = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var data in doc.Root?.Elements("data") ?? Enumerable.Empty<XElement>())
        {
            var name = data.Attribute("name")?.Value;
            if (string.IsNullOrEmpty(name)) continue;
            d[name] = data.Element("value")?.Value ?? string.Empty;
        }
        return d;
    }

    /// <summary>Removes the resx data entry for the key if it exists. Returns true if a node was removed.</summary>
    public static bool RemoveDataKey(XDocument doc, string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        var root = doc.Root;
        if (root is null) return false;
        var data = root.Elements("data")
            .FirstOrDefault(x => x.Attribute("name")?.Value == key);
        if (data is null) return false;
        data.Remove();
        return true;
    }

    public static void UpsertDataValue(XDocument doc, string key, string value)
    {
        var root = doc.Root ?? throw new InvalidOperationException("Invalid resx: missing root.");
        var data = root.Elements("data")
            .FirstOrDefault(x => x.Attribute("name")?.Value == key);

        if (data is null)
        {
            data = new XElement("data",
                new XAttribute("name", key),
                new XAttribute(XNamespace.Xml + "space", "preserve"));
            data.Add(new XElement("value", value));
            root.Add(data);
        }
        else
        {
            if (data.Attribute(XNamespace.Xml + "space") is null)
                data.SetAttributeValue(XNamespace.Xml + "space", "preserve");
            var el = data.Element("value");
            if (el is null) data.Add(new XElement("value", value));
            else el.Value = value;
        }
    }

    public static void SaveResxWithUtf8Bom(string path, XDocument doc)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(true),
            OmitXmlDeclaration = false
        };
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        using var w = XmlWriter.Create(path, settings);
        doc.Save(w);
    }
}
