using Web.Data.Entities.Workflow;

namespace Web.Services.Workflow.Core;

public interface IApproverResolver
{
    /// <summary>
    /// Check if the given user is the active approver for the current step.
    /// When processOnlineOffline is true, priority is given to the online (active) main approver,
    /// falling back to the online backup approver if the main approver is offline.
    /// </summary>
    Task<bool> IsMainApproverAsync(ICollection<ApproverInfo> approverInfos, bool processOnlineOffline, bool isApproved, string userId);

    /// <summary>
    /// Build a dictionary mapping each ApprovalStep.Id to the hydrated list of ApproverInfo
    /// for that step, resolved from the requester's section/department context.
    /// </summary>
    Task<Dictionary<string, List<ApproverInfo>>> BuildStepApproverMapAsync(List<ApprovalStep> steps, string requesterId, int deptId, int sectionId);

    /// <summary>
    /// Return workflow steps with manager-level overrides applied in memory.
    /// Stored approval templates are not changed.
    /// </summary>
    Task<List<ApprovalStep>> ApplyManagerStepOverrideAsync(List<ApprovalStep> steps, string requesterId, int deptId, int sectionId);

    /// <summary>
    /// Return the subset of the given approver list that are currently offline.
    /// </summary>
    Task<List<string>> GetOfflineApproversAsync(List<string> approvers);

    /// <summary>
    /// Get the active approver from the given collection of approver information (priority: active main first, then active backup).
    /// </summary>
    /// <param name="approverInfos">The collection of approver information.</param>
    /// <param name="userStatusMap">A dictionary mapping usernames to their online status.</param>
    /// <returns>The username of the active approver, or an empty string if none are active.</returns>
    string GetActiveApproverAsync(ICollection<ApproverInfo> approverInfos, Dictionary<string, bool> userStatusMap);

    Task<Dictionary<string, bool>> GetApproverOnlineStatusAsync(List<string> approvers);

    ///<summary>
    /// IF this requester is a main approver of one of below things: 
    ///  - Section => return Department Id of this Section
    ///  - Department => return Department Id of BOD department (name == "BOD")
    /// </summary>
    Task<ManagerResolveResult> ManagerResolve(string username, int deptId, int sectionId);
}
