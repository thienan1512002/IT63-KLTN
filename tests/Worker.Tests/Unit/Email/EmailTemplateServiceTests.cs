using Moq;
using Worker.Services;
using Xunit;

namespace Worker.Tests.Unit.Email;

/// <summary>
/// Unit tests for EmailTemplateService — pure string processing, no I/O.
/// </summary>
public class EmailTemplateServiceTests
{
    private static EmailTemplateService CreateService()
    {
        // IHostEnvironment is only needed for LoadTemplate (file I/O).
        // ApplyTemplateData is pure and doesn't use it.
        var mockEnv = new Mock<Microsoft.Extensions.Hosting.IHostEnvironment>();
        return new EmailTemplateService(mockEnv.Object);
    }

    // ─── ApplyTemplateData ────────────────────────────────────────────────────────

    [Fact]
    public void ApplyTemplateData_ReplacesAllPlaceholders()
    {
        var service = CreateService();
        var template = "<p>Hello {{Name}}, your request {{RequestId}} is approved.</p>";
        var data = new Dictionary<string, string>
        {
            ["Name"] = "John",
            ["RequestId"] = "REQ-001",
        };

        var result = service.ApplyTemplateData(template, data);

        Assert.Equal("<p>Hello John, your request REQ-001 is approved.</p>", result);
    }

    [Fact]
    public void ApplyTemplateData_EmptyDictionary_ReturnsOriginalTemplate()
    {
        var service = CreateService();
        const string template = "<p>Hello {{Name}}</p>";

        var result = service.ApplyTemplateData(template, new Dictionary<string, string>());

        Assert.Equal(template, result);
    }

    [Fact]
    public void ApplyTemplateData_UnknownKeyInData_LeavesOtherPlaceholdersUnchanged()
    {
        var service = CreateService();
        const string template = "<p>{{Known}} and {{Unknown}}</p>";
        var data = new Dictionary<string, string> { ["Known"] = "value" };

        var result = service.ApplyTemplateData(template, data);

        // {{Unknown}} is not in data, so it stays as-is
        Assert.Equal("<p>value and {{Unknown}}</p>", result);
    }

    [Fact]
    public void ApplyTemplateData_MultipleOccurrencesOfSamePlaceholder_ReplacesAll()
    {
        var service = CreateService();
        const string template = "{{X}} and {{X}} again";
        var data = new Dictionary<string, string> { ["X"] = "hello" };

        var result = service.ApplyTemplateData(template, data);

        Assert.Equal("hello and hello again", result);
    }

    // ─── LoadTemplate ─────────────────────────────────────────────────────────────

    [Fact]
    public void LoadTemplate_FileNotFound_ThrowsFileNotFoundException()
    {
        var mockEnv = new Mock<Microsoft.Extensions.Hosting.IHostEnvironment>();
        mockEnv.Setup(e => e.ContentRootPath).Returns(@"C:\nonexistent\path");
        var service = new EmailTemplateService(mockEnv.Object);

        Assert.Throws<FileNotFoundException>(() =>
            service.LoadTemplate("Workflow/DoesNotExist.html"));
    }
}
