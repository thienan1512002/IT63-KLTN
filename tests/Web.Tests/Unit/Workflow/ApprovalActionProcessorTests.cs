using Moq;
using Web.Common;
using Web.Data.Entities.Workflow;
using Xunit;
using Web.Data.Enums.Workflow;
using Web.Services.Workflow;
using Web.Services.Workflow.Core;

namespace Web.Tests.Unit.Workflow;

public class ApprovalActionProcessorTests
{
    private readonly Mock<IApproverResolver> _mockResolver = new();
    private readonly ApprovalActionProcessor _processor;

    public ApprovalActionProcessorTests()
    {
        _processor = new ApprovalActionProcessor(_mockResolver.Object);
    }

    // ─── Builders ────────────────────────────────────────────────────────────────

    private static ApprovalRequest MakeRequest(string appNo = "REQ-001") => new()
    {
        ApplicationNo = appNo,
        DescriptionNo = appNo,
        Status = RequestStatus.Ongoing,
        Complete = false,
        Process = 1,
        FormCode = "REQ",
        FaxCode = "requester",
        RequestName = "Test",
        FullName = "Test User",
        Email = "test@test.com",
        MainApproval = "approver1",
        SectionName = "Sec",
        DeptName = "Dept",
        CurrentApprovalType = ApprovalType.Approval,
        SubmitDate = DateTime.Now,
    };

    /// <summary>
    /// Creates an ApprovalDetail. Pass <paramref name="nextStep"/> to wire up NextStepId.
    /// Pass <paramref name="returnToStep"/> to wire up ReturnToStepId.
    /// </summary>
    private static ApprovalDetail MakeStep(
        int id,
        bool isCurrentStep = false,
        bool checkBypass = false,
        ApprovalDetail nextStep = null,
        ApprovalDetail returnToStep = null,
        ApprovalType approvalType = ApprovalType.Approval,
        string approvers = "approver1") => new()
        {
            Id = id,
            IsCurrentStep = isCurrentStep,
            IsApproved = false,
            ApproverInfos = approvers.Split(',').Select(a => new ApproverInfo { UserName = a.Trim() }).ToList(),
            ApprovalType = approvalType,
            ActionTypes = ApprovalActionType.Approve | ApprovalActionType.Reject | ApprovalActionType.Return | ApprovalActionType.Confirm,
            NextStepId = nextStep?.Id,
            ReturnToStepId = returnToStep?.Id,
            CheckBypass = checkBypass,
            CurrentStatus = ApprovalStatus.Waiting,
            StepTitle = $"Step {id}",
            GroupTitle = "Group",
            StepOrder = id,
            ApprovalStepId = id.ToString(),
        };

    /// <summary>
    /// Builds a context backed by a Steps list.
    /// CurrentStep and NextStep are derived from the list at runtime.
    /// </summary>
    private static ApprovalActionContext MakeContext(
        ApprovalRequest request,
        List<ApprovalDetail> steps,
        ApprovalActionType actionType = ApprovalActionType.Approve,
        string userId = "approver1",
        string comment = "Test comment") => new()
        {
            Request = request,
            Steps = steps,
            ActionType = actionType,
            UserId = userId,
            Comment = comment,
            EditUrl = "/requests/edit/",
            DetailUrl = "/requests/details/",
        };

    // ─── Approve ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Approve_MiddleStep_AdvancesToNextStep()
    {
        // step1 (CheckBypass=true) → step2 with a different approver.
        // RecursiveByPass detects different approver → advances pointer, does NOT auto-approve step2.
        var step2 = MakeStep(2, approvers: "approver2");
        var step1 = MakeStep(1, isCurrentStep: true, checkBypass: true, nextStep: step2);
        var request = MakeRequest();

        _mockResolver
            .Setup(r => r.GetActiveApproverAsync(step2.ApproverInfos, null))
            .Returns("approver2");

        var context = MakeContext(request, [step1, step2], userId: "approver1");

        _processor.Approve(context);

        Assert.True(step1.IsApproved);
        Assert.Equal("approver1", step1.ApprovedBy);
        Assert.NotNull(step1.ApprovedDate);
        Assert.Equal("Test comment", step1.Comment);
        Assert.Equal(ApprovalStatus.Approved, step1.CurrentStatus);
        Assert.False(step1.IsCurrentStep);  // pointer moved away
        Assert.True(step2.IsCurrentStep);   // step2 is now current
        Assert.False(step2.IsApproved);     // step2 not auto-approved (different approver)
        Assert.Equal("approver2", request.MainApproval);
        Assert.Equal(RequestStatus.Ongoing, request.Status);
        Assert.False(request.Complete);
        Assert.Equal(2, request.Process);
    }

    [Fact]
    public void Approve_LastStep_CompletesRequest()
    {
        // Single step with CheckBypass=true; no NextStep → RecursiveByPass returns true → request complete.
        var step1 = MakeStep(1, isCurrentStep: true, checkBypass: true);
        var request = MakeRequest();
        var context = MakeContext(request, [step1]);

        _processor.Approve(context);

        Assert.True(step1.IsApproved);
        Assert.Equal(ApprovalStatus.Approved, step1.CurrentStatus);
        Assert.True(request.Complete);
        Assert.Equal(RequestStatus.Completed, request.Status);
        Assert.Equal(1, request.Process);
    }

    [Fact]
    public void Approve_SameApproverConsecutiveSteps_BypassesAndAdvancesToNextDifferentApproverStep()
    {
        // step1 (CheckBypass=true) → step2 (same approver "approver1", CheckBypass=true)
        //   → step3 (different approver "approver3").
        // Expected: step2 is auto-approved (bypassed); pointer lands on step3.
        var step3 = MakeStep(3, approvers: "approver3", checkBypass: true);
        var step2 = MakeStep(2, checkBypass: true, nextStep: step3, approvers: "approver1");
        var step1 = MakeStep(1, isCurrentStep: true, checkBypass: true, nextStep: step2, approvers: "approver1");
        var request = MakeRequest();

        _mockResolver
            .Setup(r => r.GetActiveApproverAsync(step2.ApproverInfos, null))
            .Returns("approver1");
        _mockResolver
            .Setup(r => r.GetActiveApproverAsync(step3.ApproverInfos, null))
            .Returns("approver3");

        var context = MakeContext(request, [step1, step2, step3], userId: "approver1");

        _processor.Approve(context);

        // step1: approved, pointer moved
        Assert.True(step1.IsApproved);
        Assert.False(step1.IsCurrentStep);

        // step2: auto-approved (bypassed), pointer moved
        Assert.True(step2.IsApproved);
        Assert.Equal("approver1", step2.ApprovedBy);
        Assert.Equal(ApprovalStatus.Approved, step2.CurrentStatus);
        Assert.False(step2.IsCurrentStep);

        // step3: now the current step, not yet approved
        Assert.True(step3.IsCurrentStep);
        Assert.False(step3.IsApproved);

        Assert.Equal("approver3", request.MainApproval);
        Assert.Equal(RequestStatus.Ongoing, request.Status);
        Assert.Equal(3, request.Process); // 2 approved + 1
    }

    [Fact]
    public void Approve_SameApproverButDiffApprovalTypeConsecutiveSteps_DoesNotBypassAndAdvancesToNextDifferentApproverStep()
    {
        // step1 (CheckBypass=true) → step2 (same approver "approver1", CheckBypass=true)
        //   → step3 (different approver "approver3").
        // Expected: step2 is not bypassed due to different approval type; pointer lands on step2.
        var step3 = MakeStep(3, approvers: "approver3", checkBypass: true);
        var step2 = MakeStep(2, checkBypass: true, nextStep: step3, approvers: "approver1", approvalType: ApprovalType.Confirmation);
        var step1 = MakeStep(1, isCurrentStep: true, checkBypass: true, nextStep: step2, approvers: "approver1", approvalType: ApprovalType.Approval);
        var request = MakeRequest();

        _mockResolver
            .Setup(r => r.GetActiveApproverAsync(step2.ApproverInfos, null))
            .Returns("approver1");
        _mockResolver
            .Setup(r => r.GetActiveApproverAsync(step3.ApproverInfos, null))
            .Returns("approver3");

        var context = MakeContext(request, [step1, step2, step3], userId: "approver1");

        _processor.Approve(context);

        // step1: approved, pointer moved
        Assert.True(step1.IsApproved);
        Assert.False(step1.IsCurrentStep);

        // step2: not bypassed due to different approval type, pointer remains
        Assert.False(step2.IsApproved);
        Assert.True(step2.IsCurrentStep);

        // step3: not yet reached
        Assert.False(step3.IsCurrentStep);
        Assert.False(step3.IsApproved);

        Assert.Equal("approver1", request.MainApproval);
        Assert.Equal(RequestStatus.Ongoing, request.Status);
        Assert.Equal(2, request.Process); // 1 approved + 1
    }

    [Fact]
    public void Approve_SameApproverLastStepBypass_CompletesRequest()
    {
        // step1 (CheckBypass=true) → step2 (same approver, CheckBypass=true, no next step).
        // Both steps share the same approver → step2 is auto-approved → no next step → complete.
        var step2 = MakeStep(2, checkBypass: true, approvers: "approver1");
        var step1 = MakeStep(1, isCurrentStep: true, checkBypass: true, nextStep: step2, approvers: "approver1");
        var request = MakeRequest();

        _mockResolver
            .Setup(r => r.GetActiveApproverAsync(step2.ApproverInfos, null))
            .Returns("approver1");

        var context = MakeContext(request, [step1, step2], userId: "approver1");

        _processor.Approve(context);

        Assert.True(step1.IsApproved);
        Assert.True(step2.IsApproved);
        Assert.True(request.Complete);
        Assert.Equal(RequestStatus.Completed, request.Status);
        Assert.Equal(2, request.Process);
    }

    // ─── Confirm ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Confirm_MiddleStep_AdvancesToNextStep()
    {
        var step2 = MakeStep(2, approvers: "approver2");
        var step1 = MakeStep(1, isCurrentStep: true, approvalType: ApprovalType.Confirmation, nextStep: step2);
        var request = MakeRequest();
        var context = MakeContext(request, [step1, step2], ApprovalActionType.Confirm);

        _processor.Confirm(context);

        Assert.True(step1.IsApproved);
        Assert.Equal(ApprovalStatus.Confirmed, step1.CurrentStatus);
        Assert.False(step1.IsCurrentStep);
        Assert.True(step2.IsCurrentStep);
        Assert.Equal(RequestStatus.Ongoing, request.Status);
        Assert.Equal(2, request.Process);
    }

    [Fact]
    public void Confirm_LastStep_CompletesRequest()
    {
        var step1 = MakeStep(1, isCurrentStep: true, approvalType: ApprovalType.Confirmation);
        var request = MakeRequest();
        var context = MakeContext(request, [step1], ApprovalActionType.Confirm);

        _processor.Confirm(context);

        Assert.Equal(ApprovalStatus.Confirmed, step1.CurrentStatus);
        Assert.True(request.Complete);
        Assert.Equal(RequestStatus.Completed, request.Status);
    }

    // ─── Reject ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Reject_SetsRejectedStatusAndDoesNotComplete()
    {
        var step1 = MakeStep(1, isCurrentStep: true);
        var request = MakeRequest();
        var context = MakeContext(request, [step1], ApprovalActionType.Reject, userId: "approver1");

        _processor.Reject(context);

        Assert.True(step1.IsApproved);
        Assert.Equal("approver1", step1.ApprovedBy);
        Assert.Equal(ApprovalStatus.Rejected, step1.CurrentStatus);
        Assert.False(request.Complete);
        Assert.Equal(RequestStatus.Rejected, request.Status);
        // Process is intentionally not updated on Reject to preserve the current step display.
    }

    // ─── Return ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Return_WithReturnToStep_MovesCurrentToReturnStep()
    {
        var returnTarget = MakeStep(1, isCurrentStep: false, approvers: "requester");
        var step2 = MakeStep(2, isCurrentStep: true, returnToStep: returnTarget);
        var request = MakeRequest();
        var context = MakeContext(request, [returnTarget, step2], ApprovalActionType.Return);

        _processor.Return(context);

        Assert.True(step2.IsApproved);
        Assert.Equal(ApprovalStatus.Returned, step2.CurrentStatus);
        Assert.False(step2.IsCurrentStep);
        Assert.True(returnTarget.IsCurrentStep);
        Assert.Equal("requester", request.MainApproval);
        Assert.Equal(RequestStatus.Returned, request.Status);
        Assert.False(request.Complete);
        // Process is intentionally not updated on Return to preserve the current step display.
    }

    [Fact]
    public void Return_WithoutReturnToStep_SetsReturnedStatusWithoutChangingCurrentStep()
    {
        var step1 = MakeStep(1, isCurrentStep: true); // ReturnToStepId = null
        var request = MakeRequest();
        var context = MakeContext(request, [step1], ApprovalActionType.Return);

        _processor.Return(context);

        Assert.Equal(ApprovalStatus.Returned, step1.CurrentStatus);
        Assert.Equal(RequestStatus.Returned, request.Status);
        Assert.True(step1.IsCurrentStep); // no ReturnToStep → pointer unchanged
    }

    [Fact]
    public void Return_SetsCommentAndApprovedBy()
    {
        var step1 = MakeStep(1, isCurrentStep: true);
        var context = MakeContext(MakeRequest(), [step1], ApprovalActionType.Return,
            userId: "approver99", comment: "Please fix this");

        _processor.Return(context);

        Assert.Equal("approver99", step1.ApprovedBy);
        Assert.Equal("Please fix this", step1.Comment);
        Assert.NotNull(step1.ApprovedDate);
    }
}
