namespace Worker.Services;

/// <summary>
/// Reads HTML template files from the Templates folder on disk.
/// </summary>
public interface IEmailTemplateService
{
    string LoadTemplate(string templatePath);
    string ApplyTemplateData(string template, Dictionary<string, string> data);
}

public class EmailTemplateService(IHostEnvironment env) : IEmailTemplateService
{
    /// <summary>
    /// Loads a template file from disk.
    /// Example: "Workflow/Returned.html" → Templates/Workflow/Returned.html
    /// </summary>
    public string LoadTemplate(string templatePath)
    {
        var fullPath = Path.Combine(env.ContentRootPath, "Templates", templatePath);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Email template not found: {fullPath}");

        var template = File.ReadAllText(fullPath);
        const string header = """
            <table role="presentation" cellpadding="0" cellspacing="0" style="margin-bottom:24px;font-family:Arial,sans-serif">
              <tr><td style="padding-right:12px"><img src="cid:nam-viet-logo" width="52" height="52" alt="Đại học Nam Việt" style="display:block;object-fit:contain"></td>
              <td><strong style="font-size:17px;color:#132b50">Đại học Nam Việt</strong><br><span style="font-size:12px;color:#687b96">Hệ thống phê duyệt</span></td></tr>
            </table>
            """;
        var bodyTag = template.IndexOf("<body", StringComparison.OrdinalIgnoreCase);
        var bodyStart = bodyTag < 0 ? -1 : template.IndexOf('>', bodyTag);
        if (bodyStart < 0) throw new InvalidOperationException($"Email template has no body: {templatePath}");
        return template.Insert(bodyStart + 1, header);
    }

    /// <summary>
    /// Replaces {{Key}} placeholders with values from the data dictionary.
    /// </summary>
    public string ApplyTemplateData(string template, Dictionary<string, string> data)
    {
        foreach (var kvp in data)
        {
            template = template.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
        }
        return template;
    }
}
