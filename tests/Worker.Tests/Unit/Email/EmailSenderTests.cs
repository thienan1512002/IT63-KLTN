using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Worker.Data;
using Worker.Services;
using Worker.Services.Handlers;
using Worker.Settings;
using Worker.Shared.Dtos;
using Worker.Shared.Enums;
using Xunit;

namespace Worker.Tests.Unit.Email;

/// <summary>
/// Unit tests for EmailSender.CreateMasterDataAsync.
/// Verifies that EmailMasterData is correctly persisted to the database.
/// SMTP sending (SendAsync) is not tested here as it requires a live SMTP server.
/// </summary>
public class EmailSenderTests
{
    private static WorkerDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<WorkerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static EmailSender CreateSender(WorkerDbContext ctx)
    {
        var mockSmtpOptions = new Mock<IOptions<SmtpSettings>>();
        mockSmtpOptions.Setup(o => o.Value).Returns(new SmtpSettings
        {
            Host = "localhost",
            Port = 587,
            FromAddress = "no-reply@test.com",
            FromName = "Test",
        });

        // NotificationEmailDispatcher is only needed for SendAsync (SMTP path), not CreateMasterDataAsync
        var mockHandler = new Mock<INotificationEmailHandler>();
        mockHandler.Setup(h => h.NotificationType).Returns(NotificationType.Workflow);
        var dispatcher = new NotificationEmailDispatcher(
            new[] { mockHandler.Object },
            Mock.Of<ILogger<NotificationEmailDispatcher>>());

        return new EmailSender(
            mockSmtpOptions.Object,
            ctx,
            dispatcher,
            Mock.Of<ILogger<EmailSender>>());
    }

    // ─── CreateMasterDataAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateMasterDataAsync_SavesEmailMasterDataWithPendingStatus()
    {
        var ctx = CreateDbContext();
        var sender = CreateSender(ctx);
        var request = new EmailRequest
        {
            ToAddress = "to@test.com",
            Subject = "[iKSN] Approval - REQ-001",
            WorkflowAction = WorkflowAction.Approval,
            NotificationType = NotificationType.Workflow,
        };

        var id = await sender.CreateMasterDataAsync(request);

        var saved = await ctx.EmailMasterDatas.FindAsync(id);
        Assert.NotNull(saved);
        Assert.Equal(EmailMasterDataStatus.Pending, saved.Status);
    }

    [Fact]
    public async Task CreateMasterDataAsync_PersistsToAndWorkflowActionCorrectly()
    {
        var ctx = CreateDbContext();
        var sender = CreateSender(ctx);
        var request = new EmailRequest
        {
            ToAddress = "approver@test.com",
            Subject = "[iKSN] Approval - REQ-001",
            WorkflowAction = WorkflowAction.Approval,
            NotificationType = NotificationType.Workflow,
        };

        var id = await sender.CreateMasterDataAsync(request);

        var saved = await ctx.EmailMasterDatas.FindAsync(id);
        Assert.Equal("approver@test.com", saved.ToAddress);
        Assert.Equal(WorkflowAction.Approval, saved.WorkflowAction);
        Assert.Equal(NotificationType.Workflow, saved.NotificationType);
        Assert.Equal("[iKSN] Approval - REQ-001", saved.Subject);
    }

    [Fact]
    public async Task CreateMasterDataAsync_MultipleCcAddresses_JoinsWithSemicolon()
    {
        var ctx = CreateDbContext();
        var sender = CreateSender(ctx);
        var request = new EmailRequest
        {
            ToAddress = "to@test.com",
            CcAddresses = ["cc1@test.com", "cc2@test.com"],
        };

        var id = await sender.CreateMasterDataAsync(request);

        var saved = await ctx.EmailMasterDatas.FindAsync(id);
        Assert.Equal("cc1@test.com;cc2@test.com", saved.CcAddresses);
    }

    [Fact]
    public async Task CreateMasterDataAsync_EmptyCcAddresses_StoresCcAddressesAsNull()
    {
        var ctx = CreateDbContext();
        var sender = CreateSender(ctx);
        var request = new EmailRequest
        {
            ToAddress = "to@test.com",
            CcAddresses = [],
        };

        var id = await sender.CreateMasterDataAsync(request);

        var saved = await ctx.EmailMasterDatas.FindAsync(id);
        Assert.Null(saved.CcAddresses);
    }

    [Fact]
    public async Task CreateMasterDataAsync_WithTemplateData_SerializesTemplateDataToJson()
    {
        var ctx = CreateDbContext();
        var sender = CreateSender(ctx);
        var request = new EmailRequest
        {
            ToAddress = "to@test.com",
            TemplateData = new Dictionary<string, string>
            {
                ["ApplicationName"] = "REQ Test",
                ["Applicant"] = "John - requester01",
            },
        };

        var id = await sender.CreateMasterDataAsync(request);

        var saved = await ctx.EmailMasterDatas.FindAsync(id);
        Assert.NotNull(saved.TemplateDataJson);
        Assert.Contains("ApplicationName", saved.TemplateDataJson);
        Assert.Contains("REQ Test", saved.TemplateDataJson);
    }

    [Fact]
    public async Task CreateMasterDataAsync_EmptyTemplateData_StoresTemplateDataJsonAsNull()
    {
        var ctx = CreateDbContext();
        var sender = CreateSender(ctx);
        var request = new EmailRequest
        {
            ToAddress = "to@test.com",
            TemplateData = [],
        };

        var id = await sender.CreateMasterDataAsync(request);

        var saved = await ctx.EmailMasterDatas.FindAsync(id);
        Assert.Null(saved.TemplateDataJson);
    }

    [Fact]
    public async Task CreateMasterDataAsync_ReturnsPositivePersistenceId()
    {
        var ctx = CreateDbContext();
        var sender = CreateSender(ctx);

        var id = await sender.CreateMasterDataAsync(new EmailRequest { ToAddress = "to@test.com" });

        Assert.True(id > 0);
    }
}
