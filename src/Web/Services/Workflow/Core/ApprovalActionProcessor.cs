using Web.Data.Enums.Workflow;
using Web.Common;
using Web.Data.Entities.Workflow;

namespace Web.Services.Workflow.Core;

public class ApprovalActionProcessor : IApprovalActionProcessor
{
    private readonly IApproverResolver _aproverResolver;

    public ApprovalActionProcessor(IApproverResolver approverResolver)
    {
        _aproverResolver = approverResolver;
    }

    public void Approve(ApprovalActionContext context)
    {
        try
        {

            var currentStep = context.CurrentStep;

            currentStep.IsApproved = true;
            currentStep.ApprovedBy = context.UserId;
            currentStep.ApprovedDate = DateTime.UtcNow;
            currentStep.Comment = context.Comment;
            currentStep.CurrentStatus = currentStep.ApprovalType.ConvertToStatus();

            ResetStepsAfterReturn(context); // Reset steps after return if any

            // Loop to consective steps and do bypass if needed, until we reach a non-bypass step or the end of the workflow
            if (context.Steps != null && context.Steps.Any())
            {
                var isComplete = RecursiveByPass(context.Steps, context.UserStatusMap);
                //context.CurrentStep = context.Steps.FirstOrDefault(s => s.IsCurrentStep);

                if (isComplete)
                {
                    context.Request.Process = context.Steps.Count(s => s.IsApproved == true);
                    context.Request.Complete = true;
                    context.Request.Status = RequestStatus.Completed;
                    return;
                }

                context.Request.Process = context.Steps.Count(s => s.IsApproved == true) + 1; // Process number should be number of approved steps + 1 (current processing step)
                context.Request.MainApproval = string.Join(",", context.CurrentStep.ApproverInfos.Select(a => a.UserName));
                context.Request.Status = RequestStatus.Ongoing;
                return;
            }

        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error during approve process", ex);
        }

    }

    /// <summary>
    ///  Loop and bypass all consecutive approval type steps that has same main approver
    /// </summary>
    private bool RecursiveByPass(List<ApprovalDetail> steps, Dictionary<string, bool> userStatusMap)
    {
        var currentStep = steps.FirstOrDefault(s => s.IsCurrentStep) ?? throw new NullReferenceException("Current step not found in steps list");

        var nextStep = steps.FirstOrDefault(s => s.Id == currentStep.NextStepId);

        // Request complete if there is no next step
        if (nextStep == null)
        {
            return true;
        }

        // Advance step pointer to next step        
        currentStep.IsCurrentStep = false;
        nextStep.IsCurrentStep = true;

        if (currentStep.CheckBypass == false)
        {
            return false;
        }

        // ApprovalType of next step must same as current step for bypass, if not, stop bypass process
        if (nextStep.ApprovalType != currentStep.ApprovalType)
            return false;

        // Check if current approver is also approver for next step, if not, stop bypass process
        if (!IsSameApprover(nextStep, userStatusMap, currentStep.ApprovedBy))
            return false;


        nextStep.IsApproved = true;
        nextStep.ApprovedBy = currentStep.ApprovedBy;
        nextStep.ApprovedDate = DateTime.UtcNow;
        nextStep.Comment = currentStep.Comment;
        nextStep.CurrentStatus = ApprovalStatus.Approved;

        var isComplete = RecursiveByPass(steps, userStatusMap);

        return isComplete;
    }

    private bool IsSameApprover(ApprovalDetail nextStep, Dictionary<string, bool> userStatusMap, string currentApprover)
    {

        var nextApprovers = nextStep.ApproverInfos.Select(a => a.UserName).ToList();

        var primaryNextApprover = _aproverResolver.GetActiveApproverAsync(nextStep.ApproverInfos, userStatusMap);

        // Check if there is any overlap between current approvers and next approvers       
        return currentApprover == primaryNextApprover;
    }

    public void Confirm(ApprovalActionContext context)
    {
        try
        {
            var currentStep = context.CurrentStep;

            currentStep.IsApproved = true;
            currentStep.ApprovedBy = context.UserId;
            currentStep.ApprovedDate = DateTime.UtcNow;
            currentStep.Comment = context.Comment;
            currentStep.CurrentStatus = ApprovalStatus.Confirmed;

            ResetStepsAfterReturn(context); // Reset steps after return if any

            var nextStep = context.NextStep;

            // If there is no next step, mark request as complete
            if (nextStep is null)
            {
                context.Request.Complete = true;
                context.Request.Status = RequestStatus.Completed;
                return;
            }

            // Update current step and next step status.
            currentStep.IsCurrentStep = false;
            nextStep.IsCurrentStep = true;

            context.Request.MainApproval = string.Join(",", nextStep.ApproverInfos.Select(a => a.UserName));
            context.Request.Status = RequestStatus.Ongoing;
            context.Request.Process += 1;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error during confirm process", ex);
        }
    }

    public void Reject(ApprovalActionContext context)
    {
        try
        {
            var currentStep = context.CurrentStep;

            currentStep.IsApproved = true;
            currentStep.ApprovedBy = context.UserId;
            currentStep.ApprovedDate = DateTime.UtcNow;
            currentStep.Comment = context.Comment;
            currentStep.CurrentStatus = ApprovalStatus.Rejected;

            context.Request.Complete = false;
            context.Request.Status = RequestStatus.Rejected;
            // Dont update request process for Returned/Rejected, since we need keep current process number for workflow list showing
            //context.Request.Process = WorkflowConstants.ApprovalStatusCode.Rejected;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error during reject process", ex);
        }
    }

    public void Return(ApprovalActionContext context)
    {
        try
        {
            var currentStep = context.CurrentStep;

            currentStep.IsApproved = true;
            currentStep.ApprovedBy = context.UserId;
            currentStep.ApprovedDate = DateTime.UtcNow;
            currentStep.Comment = context.Comment;
            currentStep.CurrentStatus = ApprovalStatus.Returned;

            context.Request.Complete = false;
            context.Request.Status = RequestStatus.Returned;

            var returnStep = context.Steps.FirstOrDefault(s => s.Id == currentStep.ReturnToStepId);
            if (returnStep != null)
            {
                returnStep.IsCurrentStep = true;
                returnStep.IsApproved = false;
                returnStep.CurrentStatus = ApprovalStatus.Waiting;
                currentStep.IsCurrentStep = false;
                context.Request.MainApproval = string.Join(",", returnStep.ApproverInfos.Select(a => a.UserName));
            }

            
            //context.Request.Process = WorkflowConstants.ApprovalStatusCode.Returned;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error during return process", ex);
        }
    }
    /// <summary>
    /// For return to middle step, when this returned step re-approve or re-confirm,
    /// we need to reset all the steps after this returned step to be unapproved and not current step, 
    /// so that the workflow can continue from this returned step
    /// </summary>
    public void ResetStepsAfterReturn(ApprovalActionContext context)
    {
        var currentStep = context.CurrentStep;

        // Find all steps that come after the current step in the workflow
        var stepsAfterCurrent = context.Steps.Where(s => s.StepOrder > currentStep.StepOrder && s.IsApproved).ToList();

        foreach (var step in stepsAfterCurrent)
        {
            step.IsApproved = false;
            step.ApprovedBy = null;
            step.ApprovedDate = null;
            step.Comment = null;
            step.CurrentStatus = ApprovalStatus.Waiting;
            step.IsCurrentStep = false; // Ensure these steps are not marked as current
        }
    }
}
