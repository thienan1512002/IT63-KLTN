using Moq;
using Worker.Services;
using Worker.Services.Handlers;
using Worker.Shared.Entities;
using Worker.Shared.Enums;
using Xunit;

namespace Worker.Tests.Unit.Email;

/// <summary>
/// Unit tests for WorkflowEmailHandler.
/// Verifies that each WorkflowAction routes to the correct HTML template path
/// and that template data is applied to the loaded template.
/// </summary>
public class WorkflowEmailHandlerTests
{
    private static (WorkflowEmailHandler handler, Mock<IEmailTemplateService> mockTemplateService) Create()
    {
        var mock = new Mock<IEmailTemplateService>();
        // Default: LoadTemplate returns the path it was called with so we can inspect it
        mock.Setup(s => s.LoadTemplate(It.IsAny<string>()))
            .Returns((string path) => $"<html>{path}</html>");
        mock.Setup(s => s.ApplyTemplateData(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .Returns((string t, Dictionary<string, string> _) => t);

        return (new WorkflowEmailHandler(mock.Object), mock);
    }

    // ─── Template path routing ────────────────────────────────────────────────────

    [Theory]
    [InlineData(WorkflowAction.Approval, "Approval")]
    [InlineData(WorkflowAction.Confirmation, "Confirmation")]
    [InlineData(WorkflowAction.Finish, "Finish")]
    [InlineData(WorkflowAction.Returned, "Returned")]
    [InlineData(WorkflowAction.Rejected, "Rejected")]
    [InlineData(WorkflowAction.Submitted, "Submitted")]
    public void BuildBody_EachWorkflowAction_LoadsCorrectTemplatePath(
        WorkflowAction action, string fileName)
    {
        var (handler, mockTemplateService) = Create();
        var expectedPath = Path.Combine("Workflow", $"{fileName}.html");
        var masterData = new EmailMasterData
        {
            WorkflowAction = action,
            TemplateDataJson = null,
        };

        handler.BuildBody(masterData);

        mockTemplateService.Verify(s => s.LoadTemplate(expectedPath), Times.Once);
    }

    [Fact]
    public void BuildBody_NullWorkflowAction_ThrowsArgumentException()
    {
        var (handler, _) = Create();
        var masterData = new EmailMasterData { WorkflowAction = null };

        Assert.Throws<ArgumentException>(() => handler.BuildBody(masterData));
    }

    [Fact]
    public void BuildBody_AppliesTemplateDataFromJson()
    {
        var mock = new Mock<IEmailTemplateService>();
        mock.Setup(s => s.LoadTemplate(It.IsAny<string>())).Returns("<p>{{Name}}</p>");

        Dictionary<string, string> capturedData = null;
        mock.Setup(s => s.ApplyTemplateData(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .Callback<string, Dictionary<string, string>>((_, d) => capturedData = d)
            .Returns("<p>John</p>");

        var handler = new WorkflowEmailHandler(mock.Object);
        var masterData = new EmailMasterData
        {
            WorkflowAction = WorkflowAction.Approval,
            TemplateDataJson = """{"Name":"John","RequestId":"REQ-001"}""",
        };

        var body = handler.BuildBody(masterData);

        Assert.Equal("<p>John</p>", body);
        Assert.NotNull(capturedData);
        Assert.Equal("John", capturedData["Name"]);
        Assert.Equal("REQ-001", capturedData["RequestId"]);
    }

    [Fact]
    public void BuildBody_NullTemplateDataJson_PassesEmptyDictionary()
    {
        var mock = new Mock<IEmailTemplateService>();
        mock.Setup(s => s.LoadTemplate(It.IsAny<string>())).Returns("<p>template</p>");

        Dictionary<string, string> capturedData = null;
        mock.Setup(s => s.ApplyTemplateData(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .Callback<string, Dictionary<string, string>>((_, d) => capturedData = d)
            .Returns("<p>template</p>");

        var handler = new WorkflowEmailHandler(mock.Object);
        var masterData = new EmailMasterData
        {
            WorkflowAction = WorkflowAction.Approval,
            TemplateDataJson = null,
        };

        handler.BuildBody(masterData);

        Assert.NotNull(capturedData);
        Assert.Empty(capturedData);
    }
}
