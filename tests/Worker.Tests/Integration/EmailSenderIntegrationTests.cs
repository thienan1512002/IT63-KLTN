using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Worker.Data;
using Worker.Services;
using Worker.Shared.Dtos;
using Worker.Shared.Enums;
using Xunit;
using Xunit.Abstractions;

namespace Worker.Tests.Integration;

/// <summary>
/// Integration tests for EmailSender against the SQL Server WorkerDb.
///
/// These tests write records to the "worker" schema.  Each test cleans up its own
/// rows using the master-data ID returned by CreateMasterDataAsync so no leftover
/// data accumulates.
///
/// Run with:
///   dotnet test --filter "Category=Integration"
/// </summary>
[Collection("WorkerIntegration")]
[Trait("Category", "Integration")]
public class EmailSenderIntegrationTests(
    WorkerIntegrationFixture fixture,
    ITestOutputHelper output)
{
    // ─── CreateMasterDataAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateMasterDataAsync_PersistsRecordToSqlServer_WithPendingStatus()
    {
        using var scope = fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var ctx = scope.ServiceProvider.GetRequiredService<WorkerDbContext>();

        var request = new EmailRequest
        {
            ToAddress = "integration-test@test.com",
            Subject = "[IntegrationTest] CreateMasterDataAsync — Pending",
            WorkflowAction = WorkflowAction.Approval,
            NotificationType = NotificationType.Workflow,
            TemplateData = new Dictionary<string, string>
            {
                ["RequestName"] = "REQ Integration Test",
                ["RequesterName"] = "Test User - tester01",
                ["DateRequest"] = DateTime.Now.ToString("dd MMM yyyy"),
                ["Link"] = "https://localhost/requests/details/REQ-INT-001",
                ["Urgent"] = "Normal",
                ["Note"] = "",
                ["ApproverReturn"] = "",
                ["BelongSite"] = "Site A",
            },
        };

        long id = 0;
        try
        {
            id = await sender.CreateMasterDataAsync(request);
            output.WriteLine($"Created EmailMasterData id={id}");

            var saved = await ctx.EmailMasterDatas.FindAsync(id);

            Assert.NotNull(saved);
            Assert.Equal(EmailMasterDataStatus.Pending, saved.Status);
            Assert.Equal("integration-test@test.com", saved.ToAddress);
            Assert.Equal(WorkflowAction.Approval, saved.WorkflowAction);
            Assert.Equal(NotificationType.Workflow, saved.NotificationType);
            Assert.Contains("IntegrationTest", saved.Subject);
            Assert.NotNull(saved.TemplateDataJson);
            Assert.Contains("REQ Integration Test", saved.TemplateDataJson);
        }
        finally
        {
            await CleanupMasterDataAsync(ctx, id);
        }
    }

    [Fact]
    public async Task CreateMasterDataAsync_MultipleCcAddresses_StoredSemicolonSeparated()
    {
        using var scope = fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var ctx = scope.ServiceProvider.GetRequiredService<WorkerDbContext>();

        var request = new EmailRequest
        {
            ToAddress = "integration-test@test.com",
            CcAddresses = ["cc1@test.com", "cc2@test.com"],
            Subject = "[IntegrationTest] CC addresses",
        };

        long id = 0;
        try
        {
            id = await sender.CreateMasterDataAsync(request);
            var saved = await ctx.EmailMasterDatas.FindAsync(id);

            Assert.Equal("cc1@test.com;cc2@test.com", saved.CcAddresses);
        }
        finally
        {
            await CleanupMasterDataAsync(ctx, id);
        }
    }

    [Fact]
    public async Task CreateMasterDataAsync_EmptyCcAddresses_StoresNull()
    {
        using var scope = fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var ctx = scope.ServiceProvider.GetRequiredService<WorkerDbContext>();

        var request = new EmailRequest
        {
            ToAddress = "integration-test@test.com",
            CcAddresses = [],
            Subject = "[IntegrationTest] Empty CC",
        };

        long id = 0;
        try
        {
            id = await sender.CreateMasterDataAsync(request);
            var saved = await ctx.EmailMasterDatas.FindAsync(id);
            Assert.Null(saved.CcAddresses);
        }
        finally
        {
            await CleanupMasterDataAsync(ctx, id);
        }
    }

    // ─── SendAsync (SMTP) ─────────────────────────────────────────────────────────

    /// <summary>
    /// Full send test — sends a real email via SMTP.
    /// Requires a valid SMTP password in appsettings.Development.json.
    /// Mark as skipped with xunit.json or --filter to exclude when SMTP is unavailable.
    /// </summary>
    [Fact]
    [Trait("Category", "Integration_SMTP")]
    public async Task SendAsync_WorkflowApprovalEmail_MarksStatusSentAndLogsSuccess()
    {
        using var scope = fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var ctx = scope.ServiceProvider.GetRequiredService<WorkerDbContext>();

        var request = new EmailRequest
        {
            ToAddress = "integration-test@test.com",
            Subject = "[IntegrationTest] Workflow Approval Email",
            WorkflowAction = WorkflowAction.Approval,
            NotificationType = NotificationType.Workflow,
            TemplateData = new Dictionary<string, string>
            {
                ["RequestName"] = "REQ Integration Test",
                ["RequesterName"] = "Test User - tester01",
                ["DateRequest"] = DateTime.Now.ToString("dd MMM yyyy HH:mm"),
                ["Link"] = "https://localhost/requests/details/REQ-INT-001",
                ["Urgent"] = "Normal",
                ["Note"] = "Integration test email — please ignore.",
                ["ApproverReturn"] = "",
                ["BelongSite"] = "Site A",
            },
        };

        long id = 0;
        try
        {
            id = await sender.CreateMasterDataAsync(request);
            output.WriteLine($"Created master data id={id}, sending...");

            await sender.SendAsync(id);
            output.WriteLine("Send completed.");

            // Reload from DB to confirm persisted changes
            ctx.ChangeTracker.Clear();
            var saved = await ctx.EmailMasterDatas
                .Include(m => m.Logs)
                .FirstAsync(m => m.Id == id);

            Assert.Equal(EmailMasterDataStatus.Sent, saved.Status);
            Assert.NotNull(saved.SentAt);
            Assert.Single(saved.Logs);
            Assert.True(saved.Logs.First().IsSuccess);
        }
        finally
        {
            await CleanupMasterDataAsync(ctx, id);
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────────

    private static async Task CleanupMasterDataAsync(WorkerDbContext ctx, long id)
    {
        if (id <= 0) return;

        var logs = ctx.EmailLogs.Where(l => l.EmailMasterDataId == id);
        ctx.EmailLogs.RemoveRange(logs);

        var master = await ctx.EmailMasterDatas.FindAsync(id);
        if (master != null) ctx.EmailMasterDatas.Remove(master);

        await ctx.SaveChangesAsync();
    }
}
