using Hangfire;
using Hangfire.Common;
using Hangfire.InMemory;
using Hangfire.States;
using Microsoft.Extensions.Logging;
using Moq;
using Worker.Jobs;
using Worker.Services;
using Worker.Shared.Dtos;
using Worker.Shared.Enums;
using Worker.Shared.Interfaces;
using Xunit;

namespace Worker.Tests.Unit.Email;

/// <summary>
/// Unit tests for EmailNotificationJob.ProcessEmailAsync.
/// Verifies that:
///   1. IEmailSender.CreateMasterDataAsync is called with the correct EmailRequest.
///   2. A SendAsync job is enqueued via Hangfire with the master data ID returned by step 1.
/// Uses Hangfire.InMemory to provide a real (in-process) job storage so the static
/// BackgroundJob.Enqueue call inside ProcessEmailAsync does not throw.
/// </summary>
[Collection("HangfireEmailJobTests")]
public class EmailNotificationJobTests : IDisposable
{
    private readonly InMemoryStorage _storage = new();

    public EmailNotificationJobTests()
    {
        // Configure the static Hangfire storage per-test-class instance.
        // The [Collection] attribute ensures these tests never run in parallel.
        GlobalConfiguration.Configuration.UseStorage(_storage);
    }

    public void Dispose()
    {
        // No explicit cleanup needed — each test instance gets its own InMemoryStorage.
    }

    private static EmailNotificationJob CreateJob(IEmailSender sender) =>
        new(sender, Mock.Of<ILogger<EmailNotificationJob>>());

    // ─── ProcessEmailAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessEmailAsync_CallsCreateMasterDataWithExactRequest()
    {
        var mockSender = new Mock<IEmailSender>();
        mockSender.Setup(s => s.CreateMasterDataAsync(It.IsAny<EmailRequest>())).ReturnsAsync(1L);
        var job = CreateJob(mockSender.Object);

        var request = new EmailRequest
        {
            ToAddress = "approver@test.com",
            Subject = "[iKSN] Approval - REQ-001",
            WorkflowAction = WorkflowAction.Approval,
            NotificationType = NotificationType.Workflow,
            TemplateData = new Dictionary<string, string> { ["Key"] = "Val" },
        };

        await job.ProcessEmailAsync(request);

        mockSender.Verify(s => s.CreateMasterDataAsync(
            It.Is<EmailRequest>(r =>
                r.ToAddress == "approver@test.com" &&
                r.WorkflowAction == WorkflowAction.Approval &&
                r.NotificationType == NotificationType.Workflow)),
            Times.Once);
    }

    [Fact]
    public async Task ProcessEmailAsync_EnqueuesSendAsyncJobWithReturnedMasterDataId()
    {
        const long masterDataId = 42L;
        var mockSender = new Mock<IEmailSender>();
        mockSender.Setup(s => s.CreateMasterDataAsync(It.IsAny<EmailRequest>())).ReturnsAsync(masterDataId);
        var job = CreateJob(mockSender.Object);

        await job.ProcessEmailAsync(new EmailRequest { ToAddress = "to@test.com" });

        var enqueuedJobs = _storage.GetMonitoringApi()
            .EnqueuedJobs("default", 0, 10);

        Assert.Single(enqueuedJobs);
        var enqueued = enqueuedJobs[0].Value;
        Assert.Equal(nameof(IEmailJob.SendAsync), enqueued.Job.Method.Name);
        Assert.Equal(masterDataId, enqueued.Job.Args[0]);
    }

    [Fact]
    public async Task ProcessEmailAsync_CalledTwice_CreatesMasterDataTwice()
    {
        var mockSender = new Mock<IEmailSender>();
        mockSender.SetupSequence(s => s.CreateMasterDataAsync(It.IsAny<EmailRequest>()))
                  .ReturnsAsync(1L)
                  .ReturnsAsync(2L);
        var job = CreateJob(mockSender.Object);
        var request = new EmailRequest { ToAddress = "to@test.com" };

        await job.ProcessEmailAsync(request);
        await job.ProcessEmailAsync(request);

        mockSender.Verify(s => s.CreateMasterDataAsync(It.IsAny<EmailRequest>()), Times.Exactly(2));

        var enqueuedJobs = _storage.GetMonitoringApi().EnqueuedJobs("default", 0, 10);
        Assert.Equal(2, enqueuedJobs.Count);
    }
}

/// <summary>
/// Serializes EmailNotificationJobTests so static Hangfire storage is not shared across parallel tests.
/// </summary>
[CollectionDefinition("HangfireEmailJobTests", DisableParallelization = true)]
public class HangfireEmailJobTestsCollection { }
