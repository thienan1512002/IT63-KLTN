using Moq;
using Web.Data.Entities.Workflow;
using Xunit;
using Web.Data.Enums.Workflow;
using Web.Services.Workflow;
using Web.Services.Workflow.Core;

namespace Web.Tests.Unit.Workflow;

public class ApprovalValidatorTests
{
    private readonly Mock<IApproverResolver> _mockResolver = new();
    private readonly ApprovalValidator _validator;

    public ApprovalValidatorTests()
    {
        _validator = new ApprovalValidator(_mockResolver.Object);
    }

    // ─── Builders ────────────────────────────────────────────────────────────────

    private static ApprovalRequest ValidRequest() => new()
    {
        ApplicationNo = "REQ-001",
        Status = RequestStatus.Ongoing,
        Complete = false,
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

    private static ApprovalDetail ValidStep() => new()
    {
        Id = 1,
        IsCurrentStep = true,
        IsApproved = false,
        ApproverInfos = [new ApproverInfo { UserName = "approver1" }],
        ApprovalType = ApprovalType.Approval,
        ActionTypes = ApprovalActionType.Approve | ApprovalActionType.Reject | ApprovalActionType.Return,
        StepTitle = "Step 1",
        GroupTitle = "Group",
        ApprovalStepId = "1",
    };

    /// <summary>
    /// Creates a fully valid context. Resolver defaults to returning true (user is main approver).
    /// </summary>
    private ApprovalActionContext ValidContext(
        ApprovalRequest request = null,
        ApprovalDetail step = null,
        ApprovalActionType actionType = ApprovalActionType.Approve,
        string userId = "approver1")
    {
        _mockResolver
            .Setup(r => r.IsMainApproverAsync(It.IsAny<ICollection<ApproverInfo>>(), It.IsAny<bool>(), It.IsAny<bool>(), userId))
            .ReturnsAsync(true);

        return new ApprovalActionContext
        {
            Request = request ?? ValidRequest(),
            Steps = new List<ApprovalDetail> { step ?? ValidStep() },
            ActionType = actionType,
            UserId = userId,
            Comment = string.Empty,
        };
    }

    // ─── Happy path ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_ValidContext_DoesNotThrow()
    {
        var context = ValidContext();
        // Should complete without throwing
        await _validator.ValidateAsync(context);
    }

    // ─── UserId validation ────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_EmptyUserId_ThrowsInvalidOperationException()
    {
        var context = ValidContext(userId: "");
        await Assert.ThrowsAsync<InvalidOperationException>(() => _validator.ValidateAsync(context));
    }

    [Fact]
    public async Task ValidateAsync_NullUserId_ThrowsInvalidOperationException()
    {
        var context = ValidContext();
        context.UserId = null;
        await Assert.ThrowsAsync<InvalidOperationException>(() => _validator.ValidateAsync(context));
    }

    // ─── Request status validation ────────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_RequestAlreadyComplete_ThrowsInvalidOperationException()
    {
        var request = ValidRequest();
        request.Complete = true;
        request.Status = RequestStatus.Completed;
        var context = ValidContext(request: request);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _validator.ValidateAsync(context));
    }

    [Fact]
    public async Task ValidateAsync_RequestStatusCompleted_ThrowsInvalidOperationException()
    {
        var request = ValidRequest();
        request.Status = RequestStatus.Completed;
        var context = ValidContext(request: request);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _validator.ValidateAsync(context));
    }

    [Fact]
    public async Task ValidateAsync_RequestStatusReturned_ThrowsInvalidOperationException()
    {
        var request = ValidRequest();
        request.Status = RequestStatus.Returned;
        var context = ValidContext(request: request);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _validator.ValidateAsync(context));
    }

    [Fact]
    public async Task ValidateAsync_RequestStatusRejected_ThrowsInvalidOperationException()
    {
        var request = ValidRequest();
        request.Status = RequestStatus.Rejected;
        var context = ValidContext(request: request);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _validator.ValidateAsync(context));
    }

    // ─── Step validation ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_NullCurrentStep_ThrowsInvalidOperationException()
    {
        var context = ValidContext();
        // No step with IsCurrentStep=true → CurrentStep returns null
        context.Steps = new List<ApprovalDetail> { new ApprovalDetail { IsCurrentStep = false } };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _validator.ValidateAsync(context));
    }

    [Fact]
    public async Task ValidateAsync_StepNotCurrent_ThrowsInvalidOperationException()
    {
        var step = ValidStep();
        step.IsCurrentStep = false;
        var context = ValidContext(step: step);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _validator.ValidateAsync(context));
    }

    [Fact]
    public async Task ValidateAsync_StepAlreadyApproved_ThrowsInvalidOperationException()
    {
        var step = ValidStep();
        step.IsApproved = true;
        var context = ValidContext(step: step);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _validator.ValidateAsync(context));
    }

    // ─── Approver validation ──────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_UserNotMainApprover_ThrowsInvalidOperationException()
    {
        const string nonApprover = "nobody";

        _mockResolver
            .Setup(r => r.IsMainApproverAsync(It.IsAny<ICollection<ApproverInfo>>(), It.IsAny<bool>(), It.IsAny<bool>(), nonApprover))
            .ReturnsAsync(false);

        var context = new ApprovalActionContext
        {
            Request = ValidRequest(),
            Steps = new List<ApprovalDetail> { ValidStep() },
            ActionType = ApprovalActionType.Approve,
            UserId = nonApprover,
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _validator.ValidateAsync(context));
    }

    // ─── Action bitmask validation ────────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_ActionNotInStepBitmask_ThrowsInvalidOperationException()
    {
        const string userId = "approver1";

        _mockResolver
            .Setup(r => r.IsMainApproverAsync(It.IsAny<ICollection<ApproverInfo>>(), It.IsAny<bool>(), It.IsAny<bool>(), userId))
            .ReturnsAsync(true);

        var step = ValidStep();
        step.ActionTypes = ApprovalActionType.Approve; // Confirm is NOT in bitmask

        var context = new ApprovalActionContext
        {
            Request = ValidRequest(),
            Steps = new List<ApprovalDetail> { step },
            ActionType = ApprovalActionType.Confirm,   // not allowed
            UserId = userId,
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _validator.ValidateAsync(context));
    }

    [Fact]
    public async Task ValidateAsync_RejectActionAllowed_DoesNotThrow()
    {
        const string userId = "approver1";

        _mockResolver
            .Setup(r => r.IsMainApproverAsync(It.IsAny<ICollection<ApproverInfo>>(), It.IsAny<bool>(), It.IsAny<bool>(), userId))
            .ReturnsAsync(true);

        var step = ValidStep();
        step.ActionTypes = ApprovalActionType.Approve | ApprovalActionType.Reject;

        var context = new ApprovalActionContext
        {
            Request = ValidRequest(),
            Steps = new List<ApprovalDetail> { step },
            ActionType = ApprovalActionType.Reject,
            UserId = userId,
            Comment = "Không đáp ứng yêu cầu.",
        };

        // Should complete without throwing
        await _validator.ValidateAsync(context);
    }
    [Fact]
    public async Task ValidateAsync_RequesterCannotApproveEvenWhenAssigned()
    {
        var context = ValidContext(userId: "requester");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _validator.ValidateAsync(context));
    }

    [Theory]
    [InlineData(ApprovalActionType.Return)]
    [InlineData(ApprovalActionType.Reject)]
    public async Task ValidateAsync_ReturnAndRejectRequireComment(ApprovalActionType action)
    {
        var context = ValidContext(actionType: action);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _validator.ValidateAsync(context));
    }
}
