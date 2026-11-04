using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Web.Data.DbContexts;
using Web.Data.Entities.Workflow;
using Web.Data.Enums.Workflow;
using Web.Models.Dtos.Notification;
using Web.Services.Workflow;
using Web.Services.Workflow.Core;
using Worker.Shared.Enums;
using Xunit;

namespace Web.Tests.Unit.Workflow;

/// <summary>
/// Unit tests for ApprovalNotifier.
/// Verifies that the correct WorkflowAction and EmailContent are forwarded to
/// IWorkflowNotificationService (mail + NotiToast) for each workflow event.
/// </summary>
public class ApprovalNotifierTests
{
    private readonly Mock<IWorkflowNotificationService> _mockNotificationService = new();

    // ─── Infrastructure ───────────────────────────────────────────────────────────

    private static ApplicationDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    private ApprovalNotifier CreateNotifier(ApplicationDbContext ctx) =>
        new(ctx, _mockNotificationService.Object);

    private static ApprovalRequest MakeRequest(
        int requestId = 1,
        string appNo = "REQ-001",
        string requester = "requester01",
        RequestStatus status = RequestStatus.Ongoing) => new()
        {
            Id = requestId,
            ApplicationNo = appNo,
            DescriptionNo = appNo,
            Status = status,
            Complete = false,
            FormCode = "REQ",
            FaxCode = requester,
            RequestName = "REQ Test",
            FullName = "Test User",
            Email = "test@test.com",
            PhoneNumber = "0123",
            Position = "Staff",
            DeptName = "Dept",
            DeptId = 1,
            SectionName = "Section",
            SectionId = 1,
            MainApproval = requester,
            CurrentApprovalType = ApprovalType.Approval,
            SubmitDate = DateTime.Now,
        };

    private static ApprovalDetail MakeStep(
        ApprovalType type = ApprovalType.Approval,
        string approvers = "approver1",
        int stepOrder = 1,
        bool processOnlineOffline = false,
        bool isCurrentStep = true) => new()
        {
            ApprovalStepId = stepOrder.ToString(),
            IsCurrentStep = isCurrentStep,
            IsApproved = false,
            ApproverInfos = approvers.Split(',').Select(a => new ApproverInfo { UserName = a.Trim() }).ToList(),
            ApprovalType = type,
            ProcessOnlineOffline = processOnlineOffline,
            StepOrder = stepOrder,
            StepTitle = $"Step {stepOrder}",
            GroupTitle = "Group",
            CurrentStatus = ApprovalStatus.Waiting,
            ActionTypes = ApprovalActionType.Approve | ApprovalActionType.Reject,
        };

    // ─── NotifySubmittedAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task NotifySubmittedAsync_SendsSubmittedActionWithRequesterAsReceiver()
    {
        var ctx = CreateDbContext();
        var notifier = CreateNotifier(ctx);

        var context = new ApprovalActionContext
        {
            Request = MakeRequest(requester: "requester01"),
            Steps = new List<ApprovalDetail> { MakeStep() },
            UserId = "requester01",
            DetailUrl = "/requests/details/",
        };

        _mockNotificationService
            .Setup(s => s.SendMailAndNotificationAsync(It.IsAny<WorkflowAction>(), It.IsAny<EmailContent>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await notifier.NotifySubmittedAsync(context);

        _mockNotificationService.Verify(s => s.SendMailAndNotificationAsync(
            WorkflowAction.Submitted,
            It.Is<EmailContent>(e => e.Receivers.Contains("requester01")),
            It.IsAny<string>()), Times.Once);
    }

    // ─── NotifyApprovedAsync — first step ─────────────────────────────────────────

    [Fact]
    public async Task NotifyApprovedAsync_FirstStep_ApprovalType_SendsApprovalActionToCurrentStepApprovers()
    {
        var ctx = CreateDbContext();
        var notifier = CreateNotifier(ctx);

        var context = new ApprovalActionContext
        {
            Request = MakeRequest(status: RequestStatus.Ongoing),
            Steps = new List<ApprovalDetail> { MakeStep(type: ApprovalType.Approval, approvers: "approver1,approver2") },
            UserId = "requester01",
            DetailUrl = "/requests/details/",
        };

        _mockNotificationService
            .Setup(s => s.SendMailAndNotificationAsync(It.IsAny<WorkflowAction>(), It.IsAny<EmailContent>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await notifier.NotifyApprovedAsync(context);

        _mockNotificationService.Verify(s => s.SendMailAndNotificationAsync(
            WorkflowAction.Approval,
            It.Is<EmailContent>(e => e.Receivers.Contains("approver1") && e.Receivers.Contains("approver2")),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task NotifyApprovedAsync_FirstStep_ConfirmationType_SendsConfirmationAction()
    {
        var ctx = CreateDbContext();
        var notifier = CreateNotifier(ctx);

        var context = new ApprovalActionContext
        {
            Request = MakeRequest(status: RequestStatus.Ongoing),
            Steps = new List<ApprovalDetail> { MakeStep(type: ApprovalType.Confirmation, approvers: "confirmer1") },
            UserId = "requester01",
            DetailUrl = "/requests/details/",
        };

        _mockNotificationService
            .Setup(s => s.SendMailAndNotificationAsync(It.IsAny<WorkflowAction>(), It.IsAny<EmailContent>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await notifier.NotifyApprovedAsync(context);

        _mockNotificationService.Verify(s => s.SendMailAndNotificationAsync(
            WorkflowAction.Confirmation,
            It.Is<EmailContent>(e => e.Receivers.Contains("confirmer1")),
            It.IsAny<string>()), Times.Once);
    }

    // ─── NotifyApprovedAsync — non-first step ─────────────────────────────────────

    [Fact]
    public async Task NotifyApprovedAsync_NonFirstStep_SendsToNextStepApprovers()
    {
        var ctx = CreateDbContext();
        var notifier = CreateNotifier(ctx);

        // After approval, the processor has already advanced IsCurrentStep to the next step.
        var step1 = MakeStep(type: ApprovalType.Approval, approvers: "approver1", stepOrder: 1, isCurrentStep: false);
        var step2 = MakeStep(type: ApprovalType.Approval, approvers: "approver2", stepOrder: 2, isCurrentStep: true);

        var context = new ApprovalActionContext
        {
            Request = MakeRequest(status: RequestStatus.Ongoing),
            Steps = new List<ApprovalDetail> { step1, step2 },
            UserId = "approver1",
            DetailUrl = "/requests/details/",
        };

        _mockNotificationService
            .Setup(s => s.SendMailAndNotificationAsync(It.IsAny<WorkflowAction>(), It.IsAny<EmailContent>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await notifier.NotifyApprovedAsync(context);

        _mockNotificationService.Verify(s => s.SendMailAndNotificationAsync(
            WorkflowAction.Approval,
            It.Is<EmailContent>(e =>
                e.Receivers.Contains("approver2") &&
                !e.Receivers.Contains("approver1")),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task NotifyApprovedAsync_NonFirstStep_NextIsConfirmationType_SendsConfirmationAction()
    {
        var ctx = CreateDbContext();
        var notifier = CreateNotifier(ctx);

        // After approval, the processor has advanced IsCurrentStep to the confirmation step.
        var step1 = MakeStep(type: ApprovalType.Approval, approvers: "approver1", stepOrder: 1, isCurrentStep: false);
        var step2 = MakeStep(type: ApprovalType.Confirmation, approvers: "confirmer1", stepOrder: 2, isCurrentStep: true);

        var context = new ApprovalActionContext
        {
            Request = MakeRequest(status: RequestStatus.Ongoing),
            Steps = new List<ApprovalDetail> { step1, step2 },
            UserId = "approver1",
            DetailUrl = "/requests/details/",
        };

        _mockNotificationService
            .Setup(s => s.SendMailAndNotificationAsync(It.IsAny<WorkflowAction>(), It.IsAny<EmailContent>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await notifier.NotifyApprovedAsync(context);

        _mockNotificationService.Verify(s => s.SendMailAndNotificationAsync(
            WorkflowAction.Confirmation,
            It.Is<EmailContent>(e => e.Receivers.Contains("confirmer1")),
            It.IsAny<string>()), Times.Once);
    }

    // ─── NotifyApprovedAsync — request completed ──────────────────────────────────

    [Fact]
    public async Task NotifyApprovedAsync_RequestCompleted_SendsFinishActionToRequester()
    {
        var ctx = CreateDbContext();
        var notifier = CreateNotifier(ctx);

        var context = new ApprovalActionContext
        {
            Request = MakeRequest(requester: "requester01", status: RequestStatus.Completed),
            Steps = new List<ApprovalDetail> { MakeStep() },
            UserId = "approver1",
            DetailUrl = "/requests/details/",
        };

        _mockNotificationService
            .Setup(s => s.SendMailAndNotificationAsync(It.IsAny<WorkflowAction>(), It.IsAny<EmailContent>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await notifier.NotifyApprovedAsync(context);

        _mockNotificationService.Verify(s => s.SendMailAndNotificationAsync(
            WorkflowAction.Finish,
            It.Is<EmailContent>(e => e.FaxCode == "requester01"),
            It.IsAny<string>()), Times.Once);
    }

    // ─── NotifyReturnedOrRejectedAsync ────────────────────────────────────────────

    [Fact]
    public async Task NotifyReturnedOrRejectedAsync_Return_SendsReturnedActionWithCommentToRequester()
    {
        var ctx = CreateDbContext();
        var notifier = CreateNotifier(ctx);

        var context = new ApprovalActionContext
        {
            Request = MakeRequest(requester: "requester01", status: RequestStatus.Returned),
            Steps = new List<ApprovalDetail> { MakeStep() },
            UserId = "approver1",
            EditUrl = "/requests/edit/",
            Comment = "Please revise",
        };

        _mockNotificationService
            .Setup(s => s.SendMailAndNotificationAsync(It.IsAny<WorkflowAction>(), It.IsAny<EmailContent>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await notifier.NotifyReturnedOrRejectedAsync(context, isRejection: false);

        _mockNotificationService.Verify(s => s.SendMailAndNotificationAsync(
            WorkflowAction.Returned,
            It.Is<EmailContent>(e =>
                e.Receivers.Contains("requester01") &&
                e.Comment == "Please revise"),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task NotifyReturnedOrRejectedAsync_Rejection_SendsRejectedActionToRequester()
    {
        var ctx = CreateDbContext();
        var notifier = CreateNotifier(ctx);

        var context = new ApprovalActionContext
        {
            Request = MakeRequest(requester: "requester01", status: RequestStatus.Rejected),
            Steps = new List<ApprovalDetail> { MakeStep() },
            UserId = "approver1",
            DetailUrl = "/requests/details/",
            Comment = "Not approved",
        };

        _mockNotificationService
            .Setup(s => s.SendMailAndNotificationAsync(It.IsAny<WorkflowAction>(), It.IsAny<EmailContent>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await notifier.NotifyReturnedOrRejectedAsync(context, isRejection: true);

        _mockNotificationService.Verify(s => s.SendMailAndNotificationAsync(
            WorkflowAction.Rejected,
            It.Is<EmailContent>(e => e.Receivers.Contains("requester01")),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task NotifyReturnedOrRejectedAsync_WithPreviousApprovedSteps_PopulatesCcUserNames()
    {
        var ctx = CreateDbContext();
        var appNo = "REQ-001";
        var requestId = 1;

        ctx.ApprovalRequests.Add(MakeRequest(requestId: requestId, appNo: appNo, requester: "requester01"));
        ctx.ApprovalDetails.AddRange(
            // Previously approved step
            new ApprovalDetail
            {
                RequestId = requestId,
                IsCurrentStep = false,
                IsApproved = true,
                ApproverInfos = [new ApproverInfo { UserName = "approver1" }],
                ApprovedBy = "approver1",
                StepOrder = 1,
                ApprovalStepId = "1",
                ApprovalType = ApprovalType.Approval,
                StepTitle = "Step 1",
                GroupTitle = "Group",
                CurrentStatus = ApprovalStatus.Approved,
                ActionTypes = ApprovalActionType.Approve,
            },
            // Current step
            new ApprovalDetail
            {
                RequestId = requestId,
                IsCurrentStep = true,
                IsApproved = false,
                ApproverInfos = [new ApproverInfo { UserName = "approver2" }],
                StepOrder = 2,
                ApprovalStepId = "2",
                ApprovalType = ApprovalType.Approval,
                StepTitle = "Step 2",
                GroupTitle = "Group",
                CurrentStatus = ApprovalStatus.Waiting,
                ActionTypes = ApprovalActionType.Approve | ApprovalActionType.Return,
            }
        );
        await ctx.SaveChangesAsync();

        var steps = await ctx.ApprovalDetails
            .Include(d => d.ApproverInfos)
            .Where(d => d.RequestId == requestId)
            .ToListAsync();

        var notifier = CreateNotifier(ctx);

        var context = new ApprovalActionContext
        {
            Request = MakeRequest(requestId: requestId, appNo: appNo, requester: "requester01"),
            Steps = steps,
            UserId = "approver2",
            EditUrl = "/requests/edit/",
            Comment = "Revise please",
        };

        _mockNotificationService
            .Setup(s => s.SendMailAndNotificationAsync(It.IsAny<WorkflowAction>(), It.IsAny<EmailContent>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await notifier.NotifyReturnedOrRejectedAsync(context, isRejection: false);

        _mockNotificationService.Verify(s => s.SendMailAndNotificationAsync(
            WorkflowAction.Returned,
            It.Is<EmailContent>(e => e.CcUserNames.Contains("approver1")),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task NotifyReturnedOrRejectedAsync_NoPreviousApprovedSteps_CcUserNamesIsEmpty()
    {
        var ctx = CreateDbContext();
        var notifier = CreateNotifier(ctx);

        var context = new ApprovalActionContext
        {
            Request = MakeRequest(requester: "requester01"),
            Steps = new List<ApprovalDetail> { MakeStep() },
            UserId = "approver1",
            EditUrl = "/requests/edit/",
        };

        _mockNotificationService
            .Setup(s => s.SendMailAndNotificationAsync(It.IsAny<WorkflowAction>(), It.IsAny<EmailContent>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await notifier.NotifyReturnedOrRejectedAsync(context, isRejection: false);

        _mockNotificationService.Verify(s => s.SendMailAndNotificationAsync(
            WorkflowAction.Returned,
            It.Is<EmailContent>(e => e.CcUserNames.Count == 0),
            It.IsAny<string>()), Times.Once);
    }
}
