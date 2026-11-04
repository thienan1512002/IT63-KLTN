using System.Data;
using Microsoft.EntityFrameworkCore;
using Web.Data.DbContexts;
using Web.Data.Entities.Identity;
using Web.Data.Entities.Workflow;
using Web.Data.Enums.Workflow;
using Web.Models.Dtos.Workflow;
using Web.Services.UploadFile;
using Web.Services.Workflow.Core;
using Web.Data.Entities.Notification;
using Web.Services.Workflow.Operations;

namespace Web.Services.Workflow;

public class RequestService(
    ApplicationDbContext db,
    IUploadFileService uploadFileService,
    RequestBusinessService business,
    IApprovalWorkflowBuilder workflowBuilder,
    IApprovalValidator approvalValidator,
    IApprovalActionProcessor actionProcessor,
    IRequestNumberGenerator numberGenerator,
    IApprovalNotifier notifier,
    ApprovalOperationsService operations) : IRequestService
{
    private static readonly string[] AllowedExtensions =
        [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".png", ".jpg", ".jpeg"];

    public IQueryable<RequestListItemDto> BuildMyRequestsQuery(string userName) =>
        ProjectList(db.ApprovalRequests.AsNoTracking().Where(x => x.FaxCode == userName));

    public IQueryable<RequestListItemDto> BuildPendingApprovalsQuery(string userName) =>
        ProjectList(db.ApprovalRequests.AsNoTracking().Where(x =>
            x.Status == RequestStatus.Ongoing &&
            x.ApprovalDetails.Any(d => d.IsCurrentStep && !d.IsApproved &&
                d.ApproverInfos.Any(a => a.UserName == userName))));

    public async Task<RequestDetailsDto> GetDetailsAsync(int id, string userName, bool isAdmin, CancellationToken ct = default)
    {
        var request = await RequestGraph().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy yêu cầu.");
        var viewerId = await db.Users.Where(x => x.UserName == userName).Select(x => x.Id).SingleOrDefaultAsync(ct);
        if (viewerId == null) throw new UnauthorizedAccessException("Tài khoản không còn tồn tại.");
        var canView = isAdmin || request.FaxCode == userName ||
            request.BusinessData?.EmployeeId == viewerId || request.BusinessData?.Participants.Any(x => x.EmployeeId == viewerId) == true ||
            await db.VehicleDispatches.AnyAsync(x => x.RequestId == id && x.DriverEmployeeId == viewerId, ct) ||
            request.ApprovalDetails.Any(x => x.ApproverInfos.Any(a => a.UserName == userName));
        if (!canView) throw new UnauthorizedAccessException("Bạn không có quyền xem yêu cầu này.");

        var current = request.ApprovalDetails.SingleOrDefault(x => x.IsCurrentStep && !x.IsApproved);
        var actorNames = await db.AppUsers.AsNoTracking()
            .Where(x => request.Histories.Select(h => h.ActionByUserId).Contains(x.UserName!))
            .ToDictionaryAsync(x => x.UserName!, x => x.FullName, ct);
        var dto = new RequestDetailsDto
        {
            Id = request.Id, ApplicationNo = request.ApplicationNo, RequestTypeId = request.RequestTypeId,
            RequestType = request.RequestType.Name, TemplateCode = request.BusinessData?.TemplateCode ?? request.RequestType.TemplateCode, Title = request.Title, Content = request.Content,
            CreatedBy = request.FullName, Department = request.Department.Name, Status = request.Status,
            Priority = request.Priority, RequestedDate = request.RequestedDate, Amount = request.Amount,
            Reason = request.Reason, CreatedAt = request.CreatedAt, SubmittedAt = request.SubmittedAt,
            CurrentStepOrder = current?.StepOrder,
            CurrentApprover = string.Join(", ", current?.ApproverInfos.Select(x =>
                string.IsNullOrWhiteSpace(x.FullName) ? x.UserName : x.FullName) ?? []),
            CanEdit = request.FaxCode == userName && request.Status is RequestStatus.Draft or RequestStatus.Returned,
            CanCancel = request.FaxCode == userName && CanCancel(request),
            CanApprove = request.Status == RequestStatus.Ongoing && request.FaxCode != userName &&
                current?.ApproverInfos.Any(x => x.UserName == userName) == true,
            Steps = request.ApprovalDetails.OrderBy(x => x.StepOrder).Select(x => new RequestApprovalStepDto
            {
                Id = x.Id, StepOrder = x.StepOrder, StepName = x.StepTitle,
                ApproverName = string.Join(", ", x.ApproverInfos.Select(a =>
                    string.IsNullOrWhiteSpace(a.FullName) ? a.UserName : a.FullName)),
                Status = ToStepStatus(x), Comment = x.Comment, ActionAt = x.ApprovedDate
            }).ToList(),
            Histories = request.Histories.OrderBy(x => x.ActionAt).ThenBy(x => x.Id).Select(x => new RequestHistoryDto
            {
                Id = x.Id, Action = x.Action, ActionBy = actorNames.GetValueOrDefault(x.ActionByUserId, x.ActionByUserId),
                StepName = (x.ApprovalDetailId.HasValue
                    ? request.ApprovalDetails.FirstOrDefault(d => d.Id == x.ApprovalDetailId)
                    : request.ApprovalDetails.FirstOrDefault(d => d.StepOrder == x.StepOrder))?.StepTitle ?? string.Empty,
                StepOrder = x.StepOrder, Comment = x.Comment, PreviousStatus = x.PreviousStatus,
                NewStatus = x.NewStatus, ActionAt = x.ActionAt
            }).ToList(),
            Attachments = request.Attachments.Select(x => new RequestAttachmentDto
            {
                Id = x.Id, FileName = x.FileName, FileSize = x.FileSize,
                Url = $"/api/requests/{request.Id}/attachments/{x.Id}"
            }).ToList()
        };
        await business.PopulateDetailsAsync(dto, request.BusinessData, ct);
        return dto;
    }

    public async Task<int> CreateDraftAsync(RequestEditDto input, string userName, IEnumerable<IFormFile> files, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var type = await GetActiveRequestTypeAsync(input.RequestTypeId, ct);
        if (!type.ApprovalCase.IsActive)
            throw new InvalidOperationException("Luồng phê duyệt đang tạm dừng tạo yêu cầu mới.");
        var user = await GetUserWithDepartmentAsync(userName, ct);
        var section = user.Section!;
        var department = section.Department;
        var now = DateTime.UtcNow;
        var request = new ApprovalRequest
        {
            ApplicationNo = "DRAFT-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant(),
            RequestTypeId = type.Id, ApprovalCaseId = type.ApprovalCaseId, CreatedByUserId = user.Id,
            WorkflowRevision = type.ApprovalCase.Revision,
            DepartmentId = department.Id, Title = input.Title.Trim(), Content = input.Content.Trim(),
            RequestedDate = input.RequestedDate, Priority = input.Priority, Amount = input.Amount,
            Reason = input.Reason?.Trim(), Status = RequestStatus.Draft, CreatedAt = now, SubmitDate = now,
            RequestName = type.Name, UrgencyLevel = input.Priority == RequestPriority.Urgent,
            FaxCode = user.UserName!, FullName = user.FullName, SiteId = user.SiteId,
            SiteName = user.BelongSite?.Name, SectionId = section.Id, SectionName = section.Name,
            DeptId = department.Id, DeptName = department.Name, Email = user.Email ?? string.Empty,
            Position = user.Position ?? string.Empty, PhoneNumber = user.PhoneNumber,
            FormCode = type.Code, MainApproval = string.Empty, CurrentApprovalType = ApprovalType.Approval
        };
        await business.SaveAsync(request, type, input.BusinessData, false, ct);
        ValidateFields(request, type, false);
        db.ApprovalRequests.Add(request);
        await db.SaveChangesAsync(ct);
        await AddFilesAsync(request, type, files, ct);
        AddHistory(request, userName, ApprovalAction.Created, RequestStatus.Draft, RequestStatus.Draft);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return request.Id;
    }

    public async Task UpdateDraftAsync(int id, RequestEditDto input, string userName, IEnumerable<IFormFile> files, CancellationToken ct = default)
    {
        await EnsureActiveActor(userName, ct);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var request = await db.ApprovalRequests.Include(x => x.RequestType)
            .Include(x => x.BusinessData).ThenInclude(x => x.Items)
            .Include(x => x.BusinessData).ThenInclude(x => x.Participants)
            .SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException("Không tìm thấy yêu cầu.");
        EnsureOwnerCanEdit(request, userName);
        if (request.Status == RequestStatus.Returned)
            await approvalValidator.UpdatePermissionValidateAsync(request, userName);
        if (request.Status == RequestStatus.Returned && request.RequestTypeId != input.RequestTypeId)
            throw new InvalidOperationException("Không được đổi loại yêu cầu đã gửi.");
        var type = await GetActiveRequestTypeAsync(input.RequestTypeId, ct);
        if (request.RequestTypeId != type.Id && request.Status == RequestStatus.Draft)
        {
            if (!type.ApprovalCase.IsActive)
                throw new InvalidOperationException("Luồng phê duyệt đang tạm dừng tạo yêu cầu mới.");
            request.ApprovalCaseId = type.ApprovalCaseId;
            request.WorkflowRevision = type.ApprovalCase.Revision;
        }
        request.RequestTypeId = type.Id;
        request.FormCode = type.Code; request.RequestName = type.Name; request.Title = input.Title.Trim();
        request.Content = input.Content.Trim(); request.RequestedDate = input.RequestedDate;
        request.Priority = input.Priority; request.UrgencyLevel = input.Priority == RequestPriority.Urgent;
        request.Amount = input.Amount; request.Reason = input.Reason?.Trim(); request.UpdatedAt = DateTime.UtcNow;
        await business.SaveAsync(request, type, input.BusinessData, false, ct);
        ValidateFields(request, type, false);
        await AddFilesAsync(request, type, files, ct);
        AddHistory(request, userName, ApprovalAction.Updated, request.Status, request.Status);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task SubmitAsync(int id, string userName, CancellationToken ct = default)
    {
        await EnsureActiveActor(userName, ct);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var request = await RequestGraph().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy yêu cầu.");
        EnsureOwnerCanEdit(request, userName);
        await business.SaveAsync(request, request.RequestType, RequestBusinessService.ToInput(request.BusinessData), true, ct);
        ValidateFields(request, request.RequestType);
        if (!request.RequestType.IsActive)
            throw new InvalidOperationException("Loại yêu cầu chưa có quy trình phê duyệt hợp lệ.");
        var previous = request.Status;
        request.SubmitDate = DateTime.UtcNow;
        request.SubmittedAt = request.SubmitDate;
        if (request.ApplicationNo.StartsWith("DRAFT-", StringComparison.Ordinal))
            request.ApplicationNo = await numberGenerator.GenerateAsync(request.FormCode);
        if (previous == RequestStatus.Draft)
        {
            var creator = await GetUserWithDepartmentAsync(userName, ct);
            request.SectionId = creator.Section.Id;
            request.SectionName = creator.Section.Name;
            request.DeptId = creator.Section.DepartmentId;
            request.DepartmentId = creator.Section.DepartmentId;
            request.Department = creator.Section.Department;
            request.DeptName = creator.Section.Department.Name;
            await workflowBuilder.InitiateAsync(request);
        }
        else
        {
            var returned = request.ApprovalDetails.SingleOrDefault(x => x.CurrentStatus == ApprovalStatus.Returned)
                ?? throw new InvalidOperationException("Không tìm thấy bước đã trả lại.");
            foreach (var detail in request.ApprovalDetails.Where(x => x.StepOrder >= returned.StepOrder))
            {
                detail.IsApproved = false;
                detail.ApprovedDate = null;
                detail.ApprovedBy = string.Empty;
                detail.Comment = null;
                detail.CurrentStatus = ApprovalStatus.Waiting;
                detail.IsCurrentStep = detail.Id == returned.Id;
            }
            request.Status = RequestStatus.Ongoing;
            request.Complete = false;
            request.Process = returned.StepOrder;
            request.MainApproval = string.Join(",", returned.ApproverInfos.Select(x => x.UserName));
        }
        workflowBuilder.EnsureApproversConfigured(request.ApprovalDetails.ToList());
        if (request.ApprovalDetails.SelectMany(x => x.ApproverInfos).Any(x => x.UserName == userName))
            throw new InvalidOperationException("Người tạo không được tự phê duyệt yêu cầu của mình.");
        await operations.SubmittedAsync(request, ct);
        request.CurrentStepOrder = request.ApprovalDetails.Single(x => x.IsCurrentStep).StepOrder;
        AddHistory(request, userName,
            previous == RequestStatus.Returned ? ApprovalAction.Resubmitted : ApprovalAction.Submitted,
            previous, request.Status, request.CurrentStepOrder);
        await db.SaveChangesAsync(ct);
        var context = CreateContext(request, userName, ApprovalActionType.None, null);
        await notifier.NotifySubmittedAsync(context);
        await notifier.NotifyApprovedAsync(context);
        await transaction.CommitAsync(ct);
    }

    public Task ApproveAsync(int id, string userName, string? comment, CancellationToken ct = default) =>
        ProcessAsync(id, userName, ApprovalAction.Approved, ApprovalActionType.Approve, comment, ct);
    public Task ReturnAsync(int id, string userName, string comment, CancellationToken ct = default) =>
        ProcessAsync(id, userName, ApprovalAction.Returned, ApprovalActionType.Return, RequireComment(comment), ct);
    public Task RejectAsync(int id, string userName, string comment, CancellationToken ct = default) =>
        ProcessAsync(id, userName, ApprovalAction.Rejected, ApprovalActionType.Reject, RequireComment(comment), ct);

    public async Task CancelAsync(int id, string userName, string? comment, CancellationToken ct = default)
    {
        await EnsureActiveActor(userName, ct);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var request = await RequestGraph().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy yêu cầu.");
        if (request.FaxCode != userName || !CanCancel(request))
            throw new InvalidOperationException("Yêu cầu không thể hủy ở trạng thái hiện tại.");
        var previous = request.Status;
        request.Status = RequestStatus.Cancelled; request.IsRemoved = true; request.CurrentStepOrder = null;
        await operations.ReleasedAsync(request.Id, ct);
        request.MainApproval = string.Empty;
        foreach (var detail in request.ApprovalDetails) detail.IsCurrentStep = false;
        AddHistory(request, userName, ApprovalAction.Cancelled, previous, request.Status, comment: comment);
        foreach (var recipient in request.ApprovalDetails.SelectMany(x => x.ApproverInfos).Select(x => x.UserName).Distinct())
            db.NotiToasts.Add(new NotiToast {
                Title = "Yêu cầu đã hủy", Content = $"Yêu cầu {request.ApplicationNo} đã được người tạo hủy.",
                SendFor = recipient, Url = $"/Requests/Details/{request.Id}", RequestId = request.Id
            });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private async Task ProcessAsync(int id, string userName, ApprovalAction historyAction,
        ApprovalActionType actionType, string? comment, CancellationToken ct)
    {
        await EnsureActiveActor(userName, ct);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var request = await RequestGraph().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy yêu cầu.");
        if (request.FaxCode == userName)
            throw new InvalidOperationException("Người tạo không được tự phê duyệt yêu cầu của mình.");
        var previous = request.Status;
        var current = request.ApprovalDetails.SingleOrDefault(x => x.IsCurrentStep)
            ?? throw new InvalidOperationException("Không có bước phê duyệt hiện tại.");
        var context = CreateContext(request, userName, actionType, comment);
        await approvalValidator.ValidateAsync(context);
        if (actionType == ApprovalActionType.Approve) actionProcessor.Approve(context);
        else if (actionType == ApprovalActionType.Return) actionProcessor.Return(context);
        else actionProcessor.Reject(context);
        if (actionType == ApprovalActionType.Return)
        {
            request.Status = RequestStatus.Returned; request.Complete = false;
            foreach (var detail in request.ApprovalDetails) detail.IsCurrentStep = false;
        }
        request.CurrentStepOrder = request.Status == RequestStatus.Ongoing
            ? request.ApprovalDetails.SingleOrDefault(x => x.IsCurrentStep)?.StepOrder : null;
        if (request.Status == RequestStatus.Completed) request.CompletedAt = DateTime.UtcNow;
        if (request.Status == RequestStatus.Completed) await operations.CompletedAsync(request, ct);
        else if (request.Status is RequestStatus.Returned or RequestStatus.Rejected) await operations.ReleasedAsync(request.Id, ct);
        if (request.Status != RequestStatus.Ongoing)
        {
            foreach (var detail in request.ApprovalDetails) detail.IsCurrentStep = false;
            request.MainApproval = string.Empty;
        }
        AddHistory(request, userName, historyAction, previous, request.Status, current.StepOrder, comment, current.Id);
        if (historyAction == ApprovalAction.Approved && request.Status == RequestStatus.Ongoing)
            db.NotiToasts.Add(new NotiToast {
                Title = "Yêu cầu đã qua một bước", Content = $"Yêu cầu {request.ApplicationNo} đã hoàn thành bước {current.StepOrder}.",
                SendFor = request.FaxCode, Url = $"/Requests/Details/{request.Id}", RequestId = request.Id
            });
        await db.SaveChangesAsync(ct);
        if (actionType == ApprovalActionType.Approve) await notifier.NotifyApprovedAsync(context);
        else await notifier.NotifyReturnedOrRejectedAsync(context, actionType == ApprovalActionType.Reject);
        await transaction.CommitAsync(ct);
    }

    private async Task EnsureActiveActor(string userName, CancellationToken ct)
    {
        if (!await db.Users.AnyAsync(x => x.UserName == userName && x.IsActive &&
            (x.LockoutEnd == null || x.LockoutEnd <= DateTimeOffset.UtcNow), ct))
            throw new UnauthorizedAccessException("Tài khoản không còn quyền thao tác.");
    }

    private IQueryable<ApprovalRequest> RequestGraph() => db.ApprovalRequests
        .Include(x => x.RequestType).Include(x => x.ApprovalCase).Include(x => x.CreatedByUser)
        .Include(x => x.BusinessData).ThenInclude(x => x.Items)
        .Include(x => x.BusinessData).ThenInclude(x => x.Participants)
        .Include(x => x.Department).Include(x => x.Attachments).Include(x => x.Histories)
        .Include(x => x.ApprovalDetails).ThenInclude(x => x.ApproverInfos);

    private ApprovalActionContext CreateContext(ApprovalRequest request, string userName,
        ApprovalActionType actionType, string? comment)
    {
        var names = request.ApprovalDetails.SelectMany(x => x.ApproverInfos)
            .Select(x => x.UserName).Distinct().ToList();
        var online = db.AppUsers.Where(x => names.Contains(x.UserName!))
            .ToDictionary(x => x.UserName!, x => x.IsOnline);
        return new ApprovalActionContext
        {
            Request = request, Steps = request.ApprovalDetails.OrderBy(x => x.StepOrder).ToList(),
            UserStatusMap = online, UserId = userName, Comment = comment?.Trim() ?? string.Empty,
            ActionType = actionType, EditUrl = "/Requests/Edit/", DetailUrl = "/Requests/Details/"
        };
    }

    private async Task<RequestType> GetActiveRequestTypeAsync(int id, CancellationToken ct) =>
        await db.RequestTypes.Include(x => x.ApprovalCase)
            .SingleOrDefaultAsync(x => x.Id == id && x.IsActive, ct)
        ?? throw new InvalidOperationException("Loại yêu cầu không tồn tại hoặc đã bị khóa.");

    private async Task<AppUser> GetUserWithDepartmentAsync(string userName, CancellationToken ct) =>
        await db.AppUsers.Include(x => x.Section).ThenInclude(x => x.Department).Include(x => x.BelongSite)
            .SingleOrDefaultAsync(x => x.UserName == userName && x.IsActive && x.SectionId != null &&
                (x.LockoutEnd == null || x.LockoutEnd <= DateTimeOffset.UtcNow) &&
                x.Section.IsActive && x.Section.Department.IsActive, ct)
        ?? throw new InvalidOperationException("Tài khoản chưa được gán đơn vị hoặc đã bị khóa.");

    private static void EnsureOwnerCanEdit(ApprovalRequest request, string userName)
    {
        if (request.FaxCode != userName || request.Status is not (RequestStatus.Draft or RequestStatus.Returned))
            throw new InvalidOperationException("Chỉ người tạo mới được sửa bản nháp hoặc yêu cầu bị trả lại.");
    }

    private static void ValidateFields(ApprovalRequest request, RequestType type, bool requireFields = true)
    {
        if (!Enum.IsDefined(request.Priority) || request.Amount < 0)
            throw new InvalidOperationException("Mức độ ưu tiên hoặc số tiền không hợp lệ.");
        if (request.Amount.HasValue && (request.Amount > 9999999999999999.99m || request.Amount != decimal.Round(request.Amount.Value, 2)))
            throw new InvalidOperationException("Số tiền vượt giới hạn hoặc có quá 2 chữ số thập phân.");
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
            throw new InvalidOperationException("Tiêu đề và nội dung là bắt buộc.");
        if (requireFields && type.RequireRequestedDate && request.RequestedDate == null)
            throw new InvalidOperationException("Ngày yêu cầu là bắt buộc.");
        if (requireFields && type.RequireAmount && request.Amount == null)
            throw new InvalidOperationException("Số tiền là bắt buộc.");
        if (requireFields && type.RequireReason && string.IsNullOrWhiteSpace(request.Reason))
            throw new InvalidOperationException("Lý do là bắt buộc.");
    }

    private async Task AddFilesAsync(ApprovalRequest request, RequestType type,
        IEnumerable<IFormFile> files, CancellationToken ct)
    {
        foreach (var file in files.Where(x => x.Length > 0))
        {
            if (!type.AllowAttachment)
                throw new InvalidOperationException("Loại yêu cầu này không cho phép tệp đính kèm.");
            var uploaded = await uploadFileService.UploadFileAsync(file, $"requests/{request.Id}", AllowedExtensions, ct);
            request.Attachments.Add(new RequestAttachment
            {
                FileName = uploaded.OriginalFileName, StoredFileName = uploaded.StoredFileName,
                ContentType = file.ContentType, FileSize = uploaded.Size, FilePath = uploaded.RelativePath
            });
        }
    }

    private static string RequireComment(string? comment) =>
        !string.IsNullOrWhiteSpace(comment) ? comment.Trim()
        : throw new InvalidOperationException("Ý kiến là bắt buộc khi trả lại hoặc từ chối.");
    private static bool CanCancel(ApprovalRequest request) =>
        request.Status is RequestStatus.Draft or RequestStatus.Returned ||
        request.Status == RequestStatus.Ongoing && request.ApprovalDetails.All(x => !x.IsApproved);
    private static ApprovalStepStatus ToStepStatus(ApprovalDetail detail) => detail.CurrentStatus switch
    {
        ApprovalStatus.Approved or ApprovalStatus.Confirmed or ApprovalStatus.Agreed => ApprovalStepStatus.Approved,
        ApprovalStatus.Returned => ApprovalStepStatus.Returned,
        ApprovalStatus.Rejected => ApprovalStepStatus.Rejected,
        _ when detail.IsCurrentStep => ApprovalStepStatus.Pending,
        _ => ApprovalStepStatus.Waiting
    };

    private static void AddHistory(ApprovalRequest request, string userName, ApprovalAction action,
        RequestStatus previous, RequestStatus next, int? stepOrder = null, string? comment = null,
        int? approvalDetailId = null) => request.Histories.Add(new ApprovalHistory
    {
        RequestCode = request.ApplicationNo, ActionByUserId = userName, Action = action,
        PreviousStatus = previous, NewStatus = next, StepOrder = stepOrder, Comment = comment,
        ApprovalDetailId = approvalDetailId, ActionAt = DateTime.UtcNow
    });

    private static IQueryable<RequestListItemDto> ProjectList(IQueryable<ApprovalRequest> query) =>
        query.OrderByDescending(x => x.CreatedAt).Select(x => new RequestListItemDto
        {
            Id = x.Id, ApplicationNo = x.ApplicationNo, RequestType = x.RequestType.Name,
            Title = x.Title, CreatedBy = x.FullName, Department = x.Department.Name,
            Status = x.Status, Priority = x.Priority, CreatedAt = x.CreatedAt, SubmittedAt = x.SubmittedAt,
            CurrentStepOrder = x.ApprovalDetails.Where(d => d.IsCurrentStep).Select(d => (int?)d.StepOrder).FirstOrDefault(),
            CurrentApprover = x.MainApproval
        });
}

