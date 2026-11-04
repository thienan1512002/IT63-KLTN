using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Web.Data.DbContexts;
using Web.Data.Entities.Identity;
using Web.Models.Dtos.Notification;
using Web.Services.Workflow;
using Worker.Shared.Dtos;
using Worker.Shared.Enums;
using Worker.Shared.Interfaces;
using Xunit;

namespace Web.Tests.Unit.Notification;

/// <summary>
/// Unit tests for WorkflowNotificationService.
/// Covers two concerns:
///   1. SaveNotificationsAsync  — NotiToast records stored in DB with correct fields.
///   2. EnqueueWorkflowEmailAsync — IBackgroundJobClient.Create called with an EmailRequest
///      that has the correct WorkflowAction, recipient address, subject, and TemplateData keys.
/// </summary>
public class WorkflowNotificationServiceTests
{
    // ─── Infrastructure ───────────────────────────────────────────────────────────

    /// <summary>
    /// Simple service factory for SaveNotificationsAsync tests.
    /// UserManager is mocked (not called in the NotiToast path).
    /// </summary>
    private static WorkflowNotificationService CreateServiceWithMockUserManager(
        ApplicationDbContext ctx,
        IBackgroundJobClient jobClient = null)
    {
        var mockUserManager = new Mock<UserManager<AppUser>>(
            Mock.Of<IUserStore<AppUser>>(), null, null, null, null, null, null, null, null);
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c[It.IsAny<string>()]).Returns((string)null);

        return new WorkflowNotificationService(
            Mock.Of<ILogger<WorkflowNotificationService>>(),
            ctx,
            jobClient ?? Mock.Of<IBackgroundJobClient>(),
            mockConfig.Object);
    }

    private static ApplicationDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    /// <summary>
    /// Service factory for EnqueueWorkflowEmailAsync tests.
    /// Uses a real InMemory UserManager so email address lookups work.
    /// </summary>
    private static async Task<WorkflowNotificationService> CreateServiceWithRealUsersAsync(
        IBackgroundJobClient jobClient,
        params (string UserName, string Email)[] users)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(o =>
            o.UseInMemoryDatabase(Guid.NewGuid().ToString())
             .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        services.AddIdentityCore<AppUser>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

        var sp = services.BuildServiceProvider();
        var ctx = sp.GetRequiredService<ApplicationDbContext>();
        var userManager = sp.GetRequiredService<UserManager<AppUser>>();

        foreach (var (userName, email) in users)
        {
            await userManager.CreateAsync(new AppUser
            {
                UserName = userName,
                Email = email,
                FullName = userName,
                EmpId = "E001",
            });
        }

        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c[It.IsAny<string>()]).Returns((string)null);

        return new WorkflowNotificationService(
            sp.GetRequiredService<ILogger<WorkflowNotificationService>>(),
            ctx,
            jobClient,
            mockConfig.Object);
    }

    private static EmailContent MakeContent(
        string faxCode = "requester01",
        string descriptionNo = "REQ-001",
        string url = "/requests/details/REQ-001",
        IEnumerable<string> receivers = null) => new()
        {
            FaxCode = faxCode,
            DescriptionNo = descriptionNo,
            RequestName = "REQ Test",
            Name = "Test User",
            Url = url,
            SubmitDate = DateTime.Now,
            Receivers = (receivers ?? []).ToHashSet(),
        };

    // ─── SaveNotificationsAsync — NotiToast persistence ───────────────────────────

    [Fact]
    public async Task SaveNotificationsAsync_Submitted_SavesNoNotiToasts()
    {
        var ctx = CreateDbContext();
        var service = CreateServiceWithMockUserManager(ctx);

        await service.SaveNotificationsAsync(WorkflowAction.Submitted, MakeContent());

        Assert.Empty(ctx.NotiToasts.ToList());
    }

    [Fact]
    public async Task SaveNotificationsAsync_Approval_SavesOneNotiToastPerReceiver()
    {
        var ctx = CreateDbContext();
        var service = CreateServiceWithMockUserManager(ctx);
        var content = MakeContent(receivers: ["approver1", "approver2"]);

        await service.SaveNotificationsAsync(WorkflowAction.Approval, content);

        var toasts = ctx.NotiToasts.ToList();
        Assert.Equal(2, toasts.Count);
        Assert.Contains(toasts, t => t.SendFor == "approver1");
        Assert.Contains(toasts, t => t.SendFor == "approver2");
    }

    [Fact]
    public async Task SaveNotificationsAsync_Confirmation_SavesOneNotiToastPerReceiver()
    {
        var ctx = CreateDbContext();
        var service = CreateServiceWithMockUserManager(ctx);
        var content = MakeContent(receivers: ["confirmer1"]);

        await service.SaveNotificationsAsync(WorkflowAction.Confirmation, content);

        var toasts = ctx.NotiToasts.ToList();
        Assert.Single(toasts);
        Assert.Equal("confirmer1", toasts[0].SendFor);
    }

    [Fact]
    public async Task SaveNotificationsAsync_Finish_SavesNotiToastForRequester()
    {
        var ctx = CreateDbContext();
        var service = CreateServiceWithMockUserManager(ctx);

        await service.SaveNotificationsAsync(WorkflowAction.Finish, MakeContent(faxCode: "requester01"));

        var toasts = ctx.NotiToasts.ToList();
        Assert.Single(toasts);
        Assert.Equal("requester01", toasts[0].SendFor);
    }

    [Fact]
    public async Task SaveNotificationsAsync_Returned_SavesNotiToastForRequester()
    {
        var ctx = CreateDbContext();
        var service = CreateServiceWithMockUserManager(ctx);
        var content = MakeContent(faxCode: "requester01", receivers: ["requester01"]);

        await service.SaveNotificationsAsync(WorkflowAction.Returned, content);

        var toasts = ctx.NotiToasts.ToList();
        Assert.Single(toasts);
        Assert.Equal("requester01", toasts[0].SendFor);
    }

    [Fact]
    public async Task SaveNotificationsAsync_Rejected_SavesNotiToastForRequester()
    {
        var ctx = CreateDbContext();
        var service = CreateServiceWithMockUserManager(ctx);
        var content = MakeContent(faxCode: "requester01", receivers: ["requester01"]);

        await service.SaveNotificationsAsync(WorkflowAction.Rejected, content);

        var toasts = ctx.NotiToasts.ToList();
        Assert.Single(toasts);
        Assert.Equal("requester01", toasts[0].SendFor);
    }


    [Fact]
    public async Task SaveNotificationsAsync_NotiToastUrl_MatchesContentUrl()
    {
        var ctx = CreateDbContext();
        var service = CreateServiceWithMockUserManager(ctx);
        var content = MakeContent(url: "/requests/details/REQ-001", receivers: ["approver1"]);

        await service.SaveNotificationsAsync(WorkflowAction.Approval, content);

        var toast = ctx.NotiToasts.Single();
        Assert.Equal("/requests/details/REQ-001", toast.Url);
    }

    // ─── EnqueueWorkflowEmailAsync — Hangfire job enqueuing ───────────────────────

    private static Mock<IBackgroundJobClient> NewMockJobClient()
    {
        var mock = new Mock<IBackgroundJobClient>();
        mock.Setup(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>())).Returns("job-1");
        return mock;
    }

    [Fact]
    public async Task EnqueueWorkflowEmailAsync_Approval_EnqueuesJobWithApprovalActionAndReceiverEmail()
    {
        var mockJobClient = NewMockJobClient();
        var service = await CreateServiceWithRealUsersAsync(mockJobClient.Object,
            ("approver1", "approver1@test.com"));
        var content = MakeContent(receivers: ["approver1"]);

        await service.EnqueueWorkflowEmailAsync(WorkflowAction.Approval, content);

        mockJobClient.Verify(c => c.Create(
            It.Is<Job>(j =>
                j.Type == typeof(IEmailJob) &&
                j.Method.Name == nameof(IEmailJob.ProcessEmailAsync) &&
                ((EmailRequest)j.Args[0]).WorkflowAction == WorkflowAction.Approval &&
                ((EmailRequest)j.Args[0]).ToAddress.Contains("approver1@test.com")),
            It.IsAny<IState>()), Times.Once);
    }

    [Fact]
    public async Task EnqueueWorkflowEmailAsync_Finish_EnqueuesJobWithRequesterEmail()
    {
        var mockJobClient = NewMockJobClient();
        var service = await CreateServiceWithRealUsersAsync(mockJobClient.Object,
            ("requester01", "requester@test.com"));
        var content = MakeContent(faxCode: "requester01");

        await service.EnqueueWorkflowEmailAsync(WorkflowAction.Finish, content);

        mockJobClient.Verify(c => c.Create(
            It.Is<Job>(j =>
                ((EmailRequest)j.Args[0]).WorkflowAction == WorkflowAction.Finish &&
                ((EmailRequest)j.Args[0]).ToAddress.Contains("requester@test.com")),
            It.IsAny<IState>()), Times.Once);
    }

    [Fact]
    public async Task EnqueueWorkflowEmailAsync_Returned_EnqueuesJobWithReturnedActionForRequester()
    {
        var mockJobClient = NewMockJobClient();
        var service = await CreateServiceWithRealUsersAsync(mockJobClient.Object,
            ("requester01", "requester@test.com"));
        var content = MakeContent(faxCode: "requester01", receivers: ["requester01"]);

        await service.EnqueueWorkflowEmailAsync(WorkflowAction.Returned, content);

        mockJobClient.Verify(c => c.Create(
            It.Is<Job>(j =>
                ((EmailRequest)j.Args[0]).WorkflowAction == WorkflowAction.Returned &&
                ((EmailRequest)j.Args[0]).ToAddress.Contains("requester@test.com")),
            It.IsAny<IState>()), Times.Once);
    }

    [Fact]
    public async Task EnqueueWorkflowEmailAsync_RecipientEmailNotInDb_DoesNotEnqueueJob()
    {
        var mockJobClient = NewMockJobClient();
        // No users seeded — email lookup returns empty list
        var service = await CreateServiceWithRealUsersAsync(mockJobClient.Object);
        var content = MakeContent(faxCode: "nobody");

        await service.EnqueueWorkflowEmailAsync(WorkflowAction.Finish, content);

        mockJobClient.Verify(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Never);
    }

    [Fact]
    public async Task EnqueueWorkflowEmailAsync_Subject_ContainsDescriptionNo()
    {
        var mockJobClient = NewMockJobClient();
        var service = await CreateServiceWithRealUsersAsync(mockJobClient.Object,
            ("approver1", "approver1@test.com"));
        var content = MakeContent(descriptionNo: "REQ-9999", receivers: ["approver1"]);

        await service.EnqueueWorkflowEmailAsync(WorkflowAction.Approval, content);

        mockJobClient.Verify(c => c.Create(
            It.Is<Job>(j => ((EmailRequest)j.Args[0]).Subject.Contains("REQ-9999")),
            It.IsAny<IState>()), Times.Once);
    }

    [Fact]
    public async Task EnqueueWorkflowEmailAsync_TemplateData_ContainsRequiredWorkflowKeys()
    {
        var mockJobClient = NewMockJobClient();
        var service = await CreateServiceWithRealUsersAsync(mockJobClient.Object,
            ("approver1", "approver1@test.com"));
        var content = MakeContent(receivers: ["approver1"]);

        await service.EnqueueWorkflowEmailAsync(WorkflowAction.Approval, content);

        mockJobClient.Verify(c => c.Create(
            It.Is<Job>(j =>
                ((EmailRequest)j.Args[0]).TemplateData.ContainsKey("RequestName") &&
                ((EmailRequest)j.Args[0]).TemplateData.ContainsKey("RequesterName") &&
                ((EmailRequest)j.Args[0]).TemplateData.ContainsKey("Link") &&
                ((EmailRequest)j.Args[0]).TemplateData.ContainsKey("Urgent")),
            It.IsAny<IState>()), Times.Once);
    }

    // ─── SendMailAndNotificationAsync — combined ──────────────────────────────────

    [Fact]
    public async Task SendMailAndNotificationAsync_Approval_SavesNotiToastsAndEnqueuesJob()
    {
        var mockJobClient = NewMockJobClient();
        // Use real users for email lookup; ctx is shared between service and NotiToast assertion
        var services = new ServiceCollection();
        services.AddLogging();
        var dbId = Guid.NewGuid().ToString();
        services.AddDbContext<ApplicationDbContext>(o =>
            o.UseInMemoryDatabase(dbId)
             .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        services.AddIdentityCore<AppUser>()
                .AddEntityFrameworkStores<ApplicationDbContext>();
        var sp = services.BuildServiceProvider();
        var ctx = sp.GetRequiredService<ApplicationDbContext>();
        var userManager = sp.GetRequiredService<UserManager<AppUser>>();
        await userManager.CreateAsync(new AppUser
        {
            UserName = "approver1",
            Email = "approver1@test.com",
            FullName = "Approver One",
            EmpId = "E001",
        });
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c[It.IsAny<string>()]).Returns((string)null);
        var service = new WorkflowNotificationService(
            sp.GetRequiredService<ILogger<WorkflowNotificationService>>(),
             ctx, mockJobClient.Object, mockConfig.Object);

        var content = MakeContent(receivers: ["approver1"]);

        await service.SendMailAndNotificationAsync(WorkflowAction.Approval, content);

        // NotiToast was saved
        Assert.Single(ctx.NotiToasts.ToList());

        // Hangfire job was enqueued
        mockJobClient.Verify(c => c.Create(
            It.Is<Job>(j => j.Type == typeof(IEmailJob)),
            It.IsAny<IState>()), Times.Once);
    }
}
