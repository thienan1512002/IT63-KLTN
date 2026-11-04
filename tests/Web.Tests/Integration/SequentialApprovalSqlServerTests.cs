using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Web.Data.DbContexts;
using Web.Data.Enums.Workflow;
using Web.Models.Dtos.Workflow;
using Web.Services.Admin;
using Web.Services.UploadFile;
using Web.Services.Workflow;
using Web.Services.Workflow.Core;
using Xunit;

namespace Web.Tests.Integration;

[CollectionDefinition("ApprovalSqlServer", DisableParallelization = true)]
public class ApprovalSqlServerCollection { }

[Collection("ApprovalSqlServer")]
[Trait("Category", "Integration")]
public class SequentialApprovalSqlServerTests
{
    private readonly string connection = Environment.GetEnvironmentVariable("APPROVAL_TEST_CONNECTION") ??
        "Server=localhost;Database=AutomaticApprovalDb_IntegrationTest;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

    private ApplicationDbContext Open()
    {
        if (!new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection).InitialCatalog.Contains("Test", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Tests require an isolated Test database.");
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connection, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)).Options);
    }

    private async Task<T> Run<T>(Func<RequestService, ApplicationDbContext, Task<T>> action)
    {
        await using var db = Open();
        await db.Database.MigrateAsync();
        var resolver = new ApproverResolver(db, Mock.Of<IUserService>());
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["Notifications:EmailEnabled"] = "false"
        }).Build();
        var notifications = new WorkflowNotificationService(NullLogger<WorkflowNotificationService>.Instance,
            db, Mock.Of<IBackgroundJobClient>(), configuration);
        var service = new RequestService(db, Mock.Of<IUploadFileService>(),
            new ApprovalWorkflowBuilder(db, resolver), new ApprovalValidator(resolver),
            new ApprovalActionProcessor(resolver), new RequestNumberGenerator(db), new ApprovalNotifier(db, notifications));
        return await action(service, db);
    }

    private Task Execute(Func<RequestService, Task> action) => Run(async (service, db) => { await action(service); return true; });

    private Task<int> Draft(int type = 2, string owner = "employee1") => Run((service, db) =>
        service.CreateDraftAsync(Input(type), owner, []));

    private static RequestEditDto Input(int type = 2) => new() {
        RequestTypeId = type, Title = "Kiểm thử SQL " + Guid.NewGuid().ToString("N"),
        Content = "Tiếng Việt: đề nghị phê duyệt", RequestedDate = DateTime.UtcNow.Date,
        Amount = 100000, Reason = "Kiểm thử tự động", Priority = RequestPriority.Normal
    };

    [Fact]
    public async Task MultipleSteps_CompleteWithHistoryAndNotifications()
    {
        var id = await Draft();
        await Execute(s => s.SubmitAsync(id, "employee1"));
        var submitted = await Run((s, db) => s.GetDetailsAsync(id, "employee1", false));
        Assert.Equal(RequestStatus.Ongoing, submitted.Status);
        Assert.Equal(2, submitted.Steps.Count);
        Assert.Contains("Tiếng Việt", submitted.Content);
        Assert.Contains("Trưởng Phòng", submitted.Steps[0].ApproverName);
        await Execute(s => s.ApproveAsync(id, "manager1", "Đồng ý bước 1"));
        await Execute(s => s.ApproveAsync(id, "director1", "Đồng ý bước 2"));
        var completed = await Run((s, db) => s.GetDetailsAsync(id, "employee1", false));
        Assert.Equal(RequestStatus.Completed, completed.Status);
        Assert.Null(completed.CurrentStepOrder);
        Assert.All(completed.Steps, x => Assert.Equal(ApprovalStepStatus.Approved, x.Status));
        Assert.Equal(4, completed.Histories.Count);
        Assert.True(await Run((s, db) => db.NotiToasts.AnyAsync(x => x.RequestId == id && x.SendFor == "employee1")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Execute(s => s.ApproveAsync(id, "director1", "Lặp")));
    }

    [Fact]
    public async Task Return_Edit_Resubmit_KeepsSnapshotAndEarlierApproval()
    {
        var id = await Draft();
        await Execute(s => s.SubmitAsync(id, "employee1"));
        var initial = await Run((s, db) => s.GetDetailsAsync(id, "employee1", false));
        await Execute(s => s.ApproveAsync(id, "manager1", "Đồng ý"));
        await Execute(s => s.ReturnAsync(id, "director1", "Bổ sung chứng từ"));
        var returned = await Run((s, db) => s.GetDetailsAsync(id, "employee1", false));
        Assert.Equal(RequestStatus.Returned, returned.Status);
        Assert.True(returned.CanEdit);
        await Execute(s => s.UpdateDraftAsync(id, Input(), "employee1", []));
        await Execute(s => s.SubmitAsync(id, "employee1"));
        var resubmitted = await Run((s, db) => s.GetDetailsAsync(id, "employee1", false));
        Assert.Equal(initial.Steps.Select(x => x.Id), resubmitted.Steps.Select(x => x.Id));
        Assert.Equal(ApprovalStepStatus.Approved, resubmitted.Steps[0].Status);
        Assert.Equal(2, resubmitted.CurrentStepOrder);
        Assert.Contains(resubmitted.Histories, x => x.Action == ApprovalAction.Returned && x.Comment == "Bổ sung chứng từ");
        await Execute(s => s.ApproveAsync(id, "director1", "Đủ hồ sơ"));
        var completed = await Run((s, db) => s.GetDetailsAsync(id, "employee1", false));
        Assert.Equal(RequestStatus.Completed, completed.Status);
        Assert.Equal(7, completed.Histories.Count);
    }

    [Fact]
    public async Task Rejection_IsTerminal_AndCommentIsRequired()
    {
        var id = await Draft(1);
        await Execute(s => s.SubmitAsync(id, "employee1"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Execute(s => s.RejectAsync(id, "manager1", "")));
        await Execute(s => s.RejectAsync(id, "manager1", "Không chấp thuận"));
        var result = await Run((s, db) => s.GetDetailsAsync(id, "employee1", false));
        Assert.Equal(RequestStatus.Rejected, result.Status);
        Assert.False(result.CanEdit);
        Assert.False(result.CanApprove);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Execute(s => s.SubmitAsync(id, "employee1")));
    }

    [Fact]
    public async Task SelfApprovalAndOutOfOrderActionAreBlocked()
    {
        var selfId = await Draft(1, "manager1");
        await Assert.ThrowsAsync<InvalidOperationException>(() => Execute(s => s.SubmitAsync(selfId, "manager1")));
        var self = await Run((s, db) => s.GetDetailsAsync(selfId, "manager1", false));
        Assert.Equal(RequestStatus.Draft, self.Status);
        Assert.Empty(self.Steps);
        var id = await Draft();
        await Execute(s => s.SubmitAsync(id, "employee1"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Execute(s => s.ApproveAsync(id, "employee1", "")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Execute(s => s.ApproveAsync(id, "director1", "")));
    }

    [Fact]
    public async Task ChangedConfiguration_DoesNotChangeSubmittedSnapshot()
    {
        var id = await Draft();
        await Execute(s => s.SubmitAsync(id, "employee1"));
        var initial = await Run((s, db) => s.GetDetailsAsync(id, "employee1", false));
        await using var config = Open();
        var setting = await config.FormApprovers.FindAsync(1);
        var previous = setting!.UserNames;
        try {
            setting.UserNames = "manager1";
            await config.SaveChangesAsync();
            await Execute(s => s.ApproveAsync(id, "manager1", ""));
            var current = await Run((s, db) => s.GetDetailsAsync(id, "employee1", false));
            Assert.Equal(initial.Steps.Select(x => x.Id), current.Steps.Select(x => x.Id));
            Assert.Contains("Giám Đốc", current.CurrentApprover);
            await Execute(s => s.ApproveAsync(id, "director1", ""));
        } finally {
            setting.UserNames = previous;
            await config.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task CancelAndPrivateDetailsEnforceOwnership()
    {
        var id = await Draft(1);
        await Execute(s => s.SubmitAsync(id, "employee1"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Run((s, db) => s.GetDetailsAsync(id, "director1", false)));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Execute(s => s.CancelAsync(id, "director1", "")));
        await Execute(s => s.CancelAsync(id, "employee1", "Không còn nhu cầu"));
        var result = await Run((s, db) => s.GetDetailsAsync(id, "employee1", false));
        Assert.Equal(RequestStatus.Cancelled, result.Status);
        Assert.Null(result.CurrentStepOrder);
        Assert.Contains(result.Histories, x => x.Action == ApprovalAction.Cancelled);
    }

    [Fact]
    public async Task ConcurrentApproval_OnlyOneActionIsRecorded()
    {
        var id = await Draft(1);
        await Execute(s => s.SubmitAsync(id, "employee1"));
        async Task<bool> Attempt()
        {
            try { await Execute(s => s.ApproveAsync(id, "manager1", "Duyệt đồng thời")); return true; }
            catch (Exception ex) when (ex is InvalidOperationException or DbUpdateException or Microsoft.Data.SqlClient.SqlException)
            { return false; }
        }
        var results = await Task.WhenAll(Attempt(), Attempt());
        Assert.Single(results.Where(x => x));
        var completed = await Run((s, db) => s.GetDetailsAsync(id, "employee1", false));
        Assert.Equal(RequestStatus.Completed, completed.Status);
        Assert.Single(completed.Histories.Where(x => x.Action == ApprovalAction.Approved));
    }

    [Fact]
    public async Task IncompleteDraft_IsSavedButCannotBeSubmitted()
    {
        var input = Input(); input.Amount = null; input.RequestedDate = null; input.Reason = null;
        var id = await Run((s, db) => s.CreateDraftAsync(input, "employee1", []));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Execute(s => s.SubmitAsync(id, "employee1")));
        var draft = await Run((s, db) => s.GetDetailsAsync(id, "employee1", false));
        Assert.Equal(RequestStatus.Draft, draft.Status);
        Assert.Empty(draft.Steps);
    }
}
