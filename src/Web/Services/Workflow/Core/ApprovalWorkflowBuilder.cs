using Microsoft.EntityFrameworkCore;
using Web.Data.DbContexts;
using Web.Data.Entities.Workflow;
using Web.Data.Enums.Workflow;
using Web.Services.Localization;

namespace Web.Services.Workflow.Core;

public class ApprovalWorkflowBuilder : IApprovalWorkflowBuilder
{
    private readonly ApplicationDbContext _context;
    private readonly IApproverResolver _approverResolver;

    public ApprovalWorkflowBuilder(ApplicationDbContext context, IApproverResolver approverResolver)
    {
        _context = context;
        _approverResolver = approverResolver;
    }

    public async Task InitiateAsync(ApprovalRequest request)
    {
        try
        {
            var steps = await LoadActiveApprovalStepsAsync(request);
            var stepRelationDict = SnapshotStepRelations(steps);

            request.ApprovalDetails = await BuildApprovalDetailsAsync(request, steps);
            SetInitialRequestState(request);

            await PersistAndLinkDetailsAsync(request.ApprovalDetails, stepRelationDict);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to initiate approval workflow: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Luồng này có xử lý fill cấp trên tự động theo người tạo: 
    /// 1. Nếu người tạo là Section Mgr (Main) => Department của người tạo duyệt 
    /// 1. Nếu người tạo là Department MGR (Main) => BOD dept duyệt
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task InitManagerAwareWorkflowAsync(ApprovalRequest request)
    {
        try
        {
            var steps = await LoadActiveApprovalStepsAsync(request);
            var stepRelationDict = SnapshotStepRelations(steps);

            request.ApprovalDetails = await BuildManagerAwareApprovalDetailsAsync(request, steps);
            SetInitialRequestState(request);

            await PersistAndLinkDetailsAsync(request.ApprovalDetails, stepRelationDict);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to initiate manager-aware approval workflow: {ex.Message}", ex);
        }
    }

    private async Task<List<ApprovalStep>> LoadActiveApprovalStepsAsync(ApprovalRequest request)
    {
        var approvalCase = await _context.ApprovalCases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CaseId == request.ApprovalCaseId);

        if (approvalCase == null || (!request.WorkflowRevision.HasValue && !approvalCase.IsActive))
            throw new InvalidDataException($"No approval case found with ID: {request.ApprovalCaseId}");

        var steps = await _context.ApprovalSteps.AsNoTracking().Include(s => s.ApprovalRole)
            .Where(s => s.ApprovalCaseId == request.ApprovalCaseId && (request.WorkflowRevision.HasValue
                ? s.Revision == request.WorkflowRevision.Value : !s.IsRemoved))
            .OrderBy(s => s.StepOrder)
            .ToListAsync();

        if (!steps.Any())
            throw new InvalidDataException($"No approval steps defined for case: {approvalCase.CaseName}");

        if (steps.Select(s => s.StepOrder).Distinct().Count() != steps.Count)
            throw new InvalidDataException("Workflow has duplicate step orders.");
        for (var i = 0; i < steps.Count; i++)
        {
            steps[i].NextStepId = i + 1 < steps.Count ? steps[i + 1].Id : null;
            steps[i].ReturnStepId = null;
            steps[i].CheckBypass = false;
            steps[i].ProcessOnlineOffline = false;
        }

        return steps;
    }

    // Keep template step links before ApprovalDetail IDs are generated.
    private static Dictionary<string, (string NextStepId, string ReturnStepId, string BypassToStepId)> SnapshotStepRelations(
        IEnumerable<ApprovalStep> steps)
    {
        return steps.ToDictionary(
            s => s.Id,
            s => (s.NextStepId, s.ReturnStepId, s.BypassToStepId));
    }

    private async Task<List<ApprovalDetail>> BuildApprovalDetailsAsync(
        ApprovalRequest request,
        List<ApprovalStep> steps)
    {
        var stepApproverMap = await _approverResolver.BuildStepApproverMapAsync(
            steps, request.FaxCode, request.DeptId, request.SectionId);

        return steps
            .Select(step => BuildApprovalDetail(step, stepApproverMap))
            .ToList();
    }

    private async Task<List<ApprovalDetail>> BuildManagerAwareApprovalDetailsAsync(
        ApprovalRequest request,
        List<ApprovalStep> steps)
    {
        var effectiveSteps = await _approverResolver.ApplyManagerStepOverrideAsync(
            steps, request.FaxCode, request.DeptId, request.SectionId);

        var stepApproverMap = await _approverResolver.BuildStepApproverMapAsync(
            effectiveSteps, request.FaxCode, request.DeptId, request.SectionId);

        return effectiveSteps
            .Select(step => BuildApprovalDetail(step, stepApproverMap))
            .ToList();
    }

    private static ApprovalDetail BuildApprovalDetail(
        ApprovalStep step,
        IReadOnlyDictionary<string, List<ApproverInfo>> stepApproverMap)
    {
        var detail = new ApprovalDetail
        {
            ApprovalStepId = step.Id,
            StepTitle = step.ApprovalRole.Title,
            GroupTitle = step.GroupTitle,
            ProcessOnlineOffline = step.ProcessOnlineOffline,
            IsCurrentStep = false,
            IsApproved = false,
            ApprovedDate = null,
            ActionTypes = step.ActionTypes,
            ApprovalType = step.ApprovalType,
            SendSubmissionNoty = step.SendSubmissionNoty,
            CheckBypass = step.CheckBypass,
            CurrentStatus = ApprovalStatus.Waiting,
            ApprovedBy = string.Empty,
            StepOrder = step.StepOrder
        };

        if (stepApproverMap.TryGetValue(step.Id, out var approvers))
            detail.ApproverInfos = approvers;

        return detail;
    }

    private static void SetInitialRequestState(ApprovalRequest request)
    {
        var firstStep = request.ApprovalDetails.OrderBy(d => d.StepOrder).First();
        firstStep.IsCurrentStep = true;

        request.MainApproval = string.Join(", ", firstStep.ApproverInfos.Select(a => a.UserName));
        request.TotalStep = request.ApprovalDetails.Count;
        request.Status = RequestStatus.Ongoing;
        request.Complete = false;
        request.Process = 1;
    }

    private async Task PersistAndLinkDetailsAsync(
        ICollection<ApprovalDetail> approvalDetails,
        Dictionary<string, (string NextStepId, string ReturnStepId, string BypassToStepId)> stepRelationDict)
    {
        // Save first so DB-generated ApprovalDetail IDs can be linked.
        _context.ApprovalDetails.AddRange(approvalDetails);
        await _context.SaveChangesAsync();

        LinkDetailSteps(approvalDetails, stepRelationDict);
    }

    /// <inheritdoc />
    public async Task ResetDetailsAsync(ApprovalRequest request)
    {
        if (request?.ApprovalDetails == null || request.ApprovalDetails.Count == 0)
            return;

        // Clear self-referencing FKs first to avoid circular dependency constraint violations
        foreach (var detail in request.ApprovalDetails)
        {
            detail.NextStepId = null;
            detail.ReturnToStepId = null;
            detail.BypassToStepId = null;
        }

        await _context.SaveChangesAsync();

        _context.ApprovalDetails.RemoveRange(request.ApprovalDetails);
        request.ApprovalDetails.Clear();
    }

    /// <inheritdoc />
    public void EnsureApproversConfigured(List<ApprovalDetail> details)
    {
        foreach (var detail in details)
        {
            if (detail.ApproverInfos == null || !detail.ApproverInfos.Any())
                throw new InvalidDataException(
                    $"No approvers found for approval step: {detail.StepTitle} (ApprovalStepId: {detail.ApprovalStepId})");
        }
    }

    // Wire NextStepId, ReturnToStepId, and BypassToStepId FKs using the saved detail IDs.
    // Must be called after SaveChangesAsync so detail.Id values are populated.
    private static void LinkDetailSteps(
        ICollection<ApprovalDetail> details,
        Dictionary<string, (string NextStepId, string ReturnStepId, string BypassToStepId)> stepRelationDict)
    {
        foreach (var detail in details)
        {
            if (!stepRelationDict.TryGetValue(detail.ApprovalStepId, out var relations))
                continue;

            if (!string.IsNullOrEmpty(relations.NextStepId))
            {
                var nextDetail = details.FirstOrDefault(d => d.ApprovalStepId == relations.NextStepId);
                if (nextDetail != null)
                    detail.NextStepId = nextDetail.Id;
            }

            if (!string.IsNullOrEmpty(relations.ReturnStepId))
            {
                var returnDetail = details.FirstOrDefault(d => d.ApprovalStepId == relations.ReturnStepId);
                if (returnDetail != null)
                    detail.ReturnToStepId = returnDetail.Id;
            }

            // We dont setting check bypass here because the approver may 
            // if (detail.CheckBypass && !string.IsNullOrEmpty(relations.BypassToStepId))
            // {
            //     var bypassToDetail = details.FirstOrDefault(d => d.ApprovalStepId == relations.BypassToStepId);
            //     if (bypassToDetail != null)
            //     {
            //         var currentMain = detail.ApproverInfos?.FirstOrDefault()?.UserName;
            //         var bypassMain = bypassToDetail.ApproverInfos?.FirstOrDefault()?.UserName;

            //         if (currentMain == bypassMain)
            //         {
            //             detail.BypassToStepId = bypassToDetail.Id;
            //             bypassToDetail.BypassFromStepId = detail.Id;
            //         }
            //         else
            //         {
            //             detail.CheckBypass = false;
            //         }
            //     }
            // }
        }
    }

    public string GetEmailRequestName(string key)
    {
        var viCulture = new System.Globalization.CultureInfo("vi-VN");
        var enCulture = new System.Globalization.CultureInfo("en-US");
        var requestName = $"{LanguageService.TranslateByCulture(key, enCulture)} / {LanguageService.TranslateByCulture(key, viCulture)}";
        return requestName;
    }
}
