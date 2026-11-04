using System.Diagnostics;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Web.Common;
using Web.Data.DbContexts;
using Web.Data.Entities.Workflow;
using Web.Data.Enums.Workflow;
using Web.Models.Dtos.Workflow;
using Web.Services.Admin;

namespace Web.Services.Workflow.Core;

public class ApproverResolver : IApproverResolver
{
    private readonly ApplicationDbContext _context;
    private readonly IUserService _userService;

    public ApproverResolver(ApplicationDbContext context, IUserService userService)
    {
        _context = context;
        _userService = userService;
    }

    /// <inheritdoc />
    public async Task<bool> IsMainApproverAsync(ICollection<ApproverInfo> approverInfos, bool processOnlineOffline, bool isApproved, string userId)
    {
        if (isApproved)
            return false;

        if (processOnlineOffline)
        {
            var activeApprover = await ResolveActiveApproverAsync(approverInfos);
            return activeApprover == userId;
        }

        return approverInfos.Any(a => a.UserName == userId);
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, List<ApproverInfo>>> BuildStepApproverMapAsync(List<ApprovalStep> steps, string requesterId, int deptId, int sectionId)
    {
        var approverData = await FetchDynamicApproverDataAsync(deptId, sectionId, steps);

        // Resolve comma-separated username strings per step (same logic as before)
        var stepUsernameDict = new Dictionary<string, List<string>>();
        foreach (var step in steps)
        {
            var role = step.ApprovalRole;
            string approversStr;

            switch (role.RoleType)
            {
                case ApprovalRoleType.Requester:
                    approversStr = requesterId;
                    break;

                case ApprovalRoleType.SectionManager:
                    if (!approverData.FromSects.TryGetValue(sectionId, out var sectMgr))
                        throw new InvalidDataException($"No Section Manager found for SectionId: {sectionId}");
                    approversStr = sectMgr.Approvers;
                    break;

                case ApprovalRoleType.DepartmentManager:
                    if (!approverData.FromDeparts.TryGetValue(deptId, out var deptMgr))
                        throw new InvalidDataException($"No Department Manager found for DepartmentId: {deptId}");
                    approversStr = deptMgr.Approvers;
                    break;
                case ApprovalRoleType.SectionMainApprover:
                    if (!approverData.FromSects.TryGetValue(sectionId, out var sectionHead) || !sectionHead.IsActive)
                        throw new InvalidDataException("Bộ phận/Bộ môn của người yêu cầu không tồn tại hoặc đã ngừng hoạt động.");
                    if (string.IsNullOrWhiteSpace(sectionHead.MainApprover))
                        throw new InvalidDataException("Bộ phận/Bộ môn của người yêu cầu chưa được gán người duyệt chính.");
                    approversStr = sectionHead.MainApprover;
                    break;
                // Section PIC
                case ApprovalRoleType.Leader:
                    if (!approverData.FromSects.TryGetValue(sectionId, out var sectLeader))
                        throw new InvalidDataException($"No Section Leader found for SectionId: {sectionId}");
                    approversStr = sectLeader.Leader;
                    break;
                // Section Deputy
                case ApprovalRoleType.DeputySectionMgr:
                    if (!approverData.FromSects.TryGetValue(sectionId, out var sectDeputy))
                        throw new InvalidDataException($"No Section Deputy found for SectionId: {sectionId}");
                    approversStr = sectDeputy.Deputy ?? string.Empty;
                    break;

                case ApprovalRoleType.NormalApprover:
                    {
                        if (!string.IsNullOrWhiteSpace(role.IdentityRoleName))
                        {
                            var roleUsers = await _context.Users
                                .Where(u => u.IsActive && u.UserName != requesterId &&
                                    u.UserRoles.Any(ur => ur.Role.Name == role.IdentityRoleName))
                                .Select(u => u.UserName)
                                .ToListAsync();
                            approversStr = string.Join(",", roleUsers);
                        }
                        else if (role.FormApprovalId.HasValue)
                        {
                            if (!approverData.FromForms.TryGetValue(role.FormApprovalId.Value, out var formApprover))
                                throw new InvalidDataException($"No Form Approver found for FormApprovalId: {role.FormApprovalId.Value}");
                            approversStr = formApprover.Approvers;
                        }
                        else
                        {
                            throw new InvalidDataException($"Normal Approver role must have FormApprovalId defined. StepId: {step.Id}");
                        }
                        break;
                    }
                case ApprovalRoleType.NotDecided:
                    approversStr = string.Empty;
                    break;
                default:
                    if (role.DepartmentId.HasValue)
                    {
                        if (!approverData.FromDeparts.TryGetValue(role.DepartmentId.Value, out var deptApprover))
                            throw new InvalidDataException($"No Department Approver found for DepartmentId: {role.DepartmentId.Value}");
                        approversStr = deptApprover.Approvers;
                    }
                    else if (role.SectionId.HasValue)
                    {
                        if (!approverData.FromSects.TryGetValue(role.SectionId.Value, out var sectApprover))
                            throw new InvalidDataException($"No Section Approver found for SectionId: {role.SectionId.Value}");
                        approversStr = sectApprover.Approvers;
                    }
                    else
                    {
                        approversStr = string.Empty;
                    }
                    break;
            }

            stepUsernameDict[step.Id] = approversStr
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }

        // Batch-fetch user info for all resolved usernames in one query
        var allUsernames = stepUsernameDict.Values
            .SelectMany(u => u)
            .Distinct()
            .ToList();

        var isVi = CultureInfo.CurrentUICulture.Name.StartsWith("vi", StringComparison.OrdinalIgnoreCase);
        var userInfoMap = await _context.Users
            .AsNoTracking()
            .Where(u => allUsernames.Contains(u.UserName) && u.IsActive &&
                (u.LockoutEnd == null || u.LockoutEnd <= DateTimeOffset.UtcNow))
            .Include(u => u.Section)
            .Select(u => new ApproverInfo
            {
                UserName = u.UserName,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Position = u.Position,
                SectionId = u.SectionId ?? 0,
                SectionName = u.Section != null ? (isVi ? u.Section.Name : u.Section.EngName) : string.Empty,
            })
            .ToDictionaryAsync(a => a.UserName);

        // Build the result mapping step ID → list of hydrated ApproverInfo.
        // IMPORTANT: create a fresh ApproverInfo instance per step entry even when the same
        // username appears in multiple steps.  Reusing the same object reference would cause
        // EF Core's change-tracker to assign the FK (ApprovalDetailId) only to the LAST step
        // that claims it, silently dropping the rows for every earlier step.
        var result = new Dictionary<string, List<ApproverInfo>>();

        foreach (var (stepId, usernames) in stepUsernameDict)
        {
            result[stepId] = usernames
                .Select(u =>
                {
                    if (userInfoMap.TryGetValue(u, out var info))
                        return new ApproverInfo
                        {
                            UserName = info.UserName,
                            FullName = info.FullName,
                            Email = info.Email,
                            PhoneNumber = info.PhoneNumber,
                            Position = info.Position,
                            SectionId = info.SectionId,
                            SectionName = info.SectionName,
                        };
                    throw new InvalidDataException($"Người phê duyệt {u} không tồn tại hoặc đã khóa.");
                })
                .ToList();
        }

        // Fill in empty lists for no decided steps to avoid null checks later
        foreach (var step in steps)
        {
            if (!result.ContainsKey(step.Id))
                result[step.Id] = new List<ApproverInfo>();
        }

        return result;
    }

    public async Task<List<ApprovalStep>> ApplyManagerStepOverrideAsync(
        List<ApprovalStep> steps,
        string requesterId,
        int deptId,
        int sectionId)
    {
        if (!steps.Any(s => s.ApprovalRole.RoleType == ApprovalRoleType.SectionManager))
            return steps;

        var managerResolve = await ManagerResolve(requesterId, deptId, sectionId);

        // Keep origninal Workflow for staff requester
        if (!managerResolve.IsManager)
            return steps;

        // Skip Deputy Step if requester is Deputy Manager/ Section Manager/ Department Manager        
        // Group by ApprovalCaseId, then order by index to reassign StepOrder, and filter out DeputySectionMgr steps
        steps = steps.Where(s=>s.ApprovalRole.RoleType != ApprovalRoleType.DeputySectionMgr)
        .GroupBy(s=>s.ApprovalCaseId)
        .SelectMany(g => g.OrderBy(s => s.StepOrder)
            .Select((s, index) =>
            {
                s.StepOrder = index + 1; // Reassign StepOrder starting from 1
                return s;
            }))
        
        .ToList();
            
        
        // Override SectionManager step with Department Manager | BOD step if requester is Section Manager | Department Manager
        return steps           
            .Select((step, i) =>
            {
                // Override SectionManager step with Department Manager | BOD step if requester is Section Manager | Department Manager
                if(managerResolve.IsDeputy == false && step.ApprovalRole.RoleType == ApprovalRoleType.SectionManager)
                {
                    return CloneStepWithRoleOverride(step, managerResolve);
                }              
                return step;
            })            
            .ToList();
    }

    /// <inheritdoc />
    public async Task<List<string>> GetOfflineApproversAsync(List<string> approvers)
    {
        try
        {           
            var offlineApprovers = await _userService.GetOfflineUserNames(approvers);
            return offlineApprovers;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get offline approvers: {ex.Message}", ex);
        }
    }

    public async Task<Dictionary<string, bool>> GetApproverOnlineStatusAsync(List<string> approvers)
    {
        var statusMap = await _context.Users
            .AsNoTracking()
            .Where(u => approvers.Contains(u.UserName))
            .Select(u => new { u.UserName, u.IsOnline })
            .ToDictionaryAsync(u => u.UserName, u => u.IsOnline);
        return statusMap;
    }

    /// <summary>
    /// Public method to get the active approver, which can be used by other services like ApprovalActionProcessor.
    /// </summary>
    /// <param name="approverInfos">The collection of approver information.</param>
    /// <param name="userStatusMap">A dictionary mapping usernames to their online status.</param>
    /// <returns>The username of the active approver, or an empty string if none are active.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public string GetActiveApproverAsync(ICollection<ApproverInfo> approverInfos, Dictionary<string, bool> userStatusMap)
    {
        try
        {
            foreach (var approver in approverInfos)
            {
                if (userStatusMap.TryGetValue(approver.UserName, out var isOnline) && isOnline)
                    return approver.UserName;
            }

            return string.Empty;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get active approver: {ex.Message}", ex);
        }
    }

    // Priority: first online approver in the list (main first, then backup).
    private async Task<string> ResolveActiveApproverAsync(ICollection<ApproverInfo> approverInfos)
    {
        var onlineMap = await _context.Users
        .AsNoTracking()
        .Where(u => approverInfos.Select(a => a.UserName).Contains(u.UserName))
            .Select(u => new { u.UserName, u.IsOnline })
            .ToDictionaryAsync(u => u.UserName, u => u.IsOnline);

        // Order approval infos by id 
        var orderedApprovers = approverInfos.OrderBy(a=>a.Id);
        foreach (var approver in orderedApprovers)
        {
            if (onlineMap.TryGetValue(approver.UserName, out var isOnline) && isOnline)
                return approver.UserName;
        }

        return string.Empty;
    }

    // Batch-fetch all department, section, and form approver data needed for the given steps.
    private async Task<ApproverDictionary> FetchDynamicApproverDataAsync(int userDeptId, int userSectId, List<ApprovalStep> steps)
    {
        var departmentIds = new HashSet<int> { userDeptId };
        var sectionIds = new HashSet<int> { userSectId };
        var formIds = new HashSet<int>();

        foreach (var step in steps)
        {
            var role = step.ApprovalRole;
            if (role.DepartmentId.HasValue) departmentIds.Add(role.DepartmentId.Value);
            if (role.SectionId.HasValue) sectionIds.Add(role.SectionId.Value);
            if (role.FormApprovalId.HasValue) formIds.Add(role.FormApprovalId.Value);
        }

        var result = new ApproverDictionary
        {
            FromDeparts = await _context.Departments
                .AsNoTracking()
                .Where(x => departmentIds.Contains(x.Id) && x.IsActive)
                .Select(x => new DynamicApproverDto
                {
                    Id = x.Id,
                    Approvers = x.MainApprover ?? "",
                })
                .ToDictionaryAsync(x => x.Id, x => x),

            FromSects = await _context.Sections
                .AsNoTracking()
                .Where(x => sectionIds.Contains(x.Id))
                .Select(x => new DynamicApproverDto
                {
                    Id = x.Id,
                    Approvers = $"{x.MainApprover}{(!string.IsNullOrEmpty(x.BackupApprover) ? $", {x.BackupApprover}" : "")}",
                    MainApprover = x.MainApprover ?? string.Empty,
                    IsActive = x.IsActive,
                    Leader = x.PIC,
                    Deputy = x.DeputyApprovers ?? string.Empty
                })
                .ToDictionaryAsync(x => x.Id, x => x),

            FromForms = await _context.FormApprovers
                .AsNoTracking()
                .Where(x => formIds.Contains(x.Id))
                .Select(x => new DynamicApproverDto
                {
                    Id = x.Id,
                    Approvers = x.UserNames
                })
                .ToDictionaryAsync(x => x.Id, x => x)
        };

        return result;
    }

    ///<summary>
    /// IF this requester is a main approver of one of below things: 
    ///  - Section => return Department Id of this Section
    ///  - Department => return Department Id of BOD department (name == "BOD")
    /// </summary>
    public async Task<ManagerResolveResult> ManagerResolve(string username, int deptId, int sectionId)
    {
        // Check if user is department manager first
        var isDeptManager = await _context.Departments
            .AsNoTracking()
            .Where(d => d.Id == deptId && d.MainApprover == username)
            .AnyAsync();

        if (isDeptManager)
        {
            var bodId = await _context.Departments
                .AsNoTracking()
                .Where(d => d.Name == RoleConstants.BOD)
                .Select(d => (int?)d.Id)
                .FirstOrDefaultAsync();

            if (!bodId.HasValue)
                throw new InvalidDataException($"No BOD department found with name: {RoleConstants.BOD}");

            return new ManagerResolveResult
            {
                IsManager = true,
                RoleType = ApprovalRoleType.DepartmentManagerWithId,
                StepTitle = "BOD",
                DepartmentId = bodId.Value,
            };
        }

        // If user isn't Department Mgr => check Section Mgr
        var sectionApprovers = await _context.Sections
            .AsNoTracking()
            .Where(s => s.Id == sectionId)
            .Select(s =>  new {s.DeputyApprovers, s.MainApprover})
            .FirstOrDefaultAsync();

        if (sectionApprovers?.MainApprover == username)
        {
            return new ManagerResolveResult
            {
                IsManager = true,
                RoleType = ApprovalRoleType.DepartmentManagerWithId,
                StepTitle = "Department Manager",
                DepartmentId = deptId,
            };
        }

        // Check Deputy Manager
        if(sectionApprovers?.DeputyApprovers != null)
        {
            var allDeputies = sectionApprovers?.DeputyApprovers.Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
            if (allDeputies.Contains(username))
            {
                return new ManagerResolveResult
                {
                    IsManager = true,
                    IsDeputy = true,                    
                };
            }
        }        

        return new ManagerResolveResult
        {
            IsManager = false
        };
    }

    private static ApprovalStep CloneStepWithRoleOverride(ApprovalStep step, ManagerResolveResult managerResolve)
    {
        return new ApprovalStep
        {
            Id = step.Id,
            ApprovalRoleId = step.ApprovalRoleId,
            ApprovalRole = CloneRoleWithManagerOverride(step.ApprovalRole, managerResolve),
            ApprovalCaseId = step.ApprovalCaseId,
            ApprovalCase = step.ApprovalCase,
            StepOrder = step.StepOrder,
            ActionTypes = step.ActionTypes,
            ApprovalType = step.ApprovalType,
            ReturnStepId = step.ReturnStepId,
            ReturnStep = step.ReturnStep,
            NextStepId = step.NextStepId,
            NextStep = step.NextStep,
            SendSubmissionNoty = step.SendSubmissionNoty,
            ProcessOnlineOffline = step.ProcessOnlineOffline,
            GroupTitle = step.GroupTitle,
            CheckBypass = step.CheckBypass,
            BypassToStepId = step.BypassToStepId,
            BypassToStep = step.BypassToStep,
            IsParentStep = step.IsParentStep,
            IsRemoved = step.IsRemoved,
        };
    }

    private static ApprovalRole CloneRoleWithManagerOverride(ApprovalRole role, ManagerResolveResult managerResolve)
    {
        return new ApprovalRole
        {
            Id = role.Id,
            Title = managerResolve.StepTitle,
            LocalizationKey = role.LocalizationKey,
            BelongSite = role.BelongSite,
            FormCode = role.FormCode,
            RoleType = managerResolve.RoleType,
            DepartmentId = managerResolve.DepartmentId,
            SectionId = managerResolve.SectionId,
            FormApprovalId = role.FormApprovalId,
            IsActive = role.IsActive,
            CreatedBy = role.CreatedBy,
            UpdatedBy = role.UpdatedBy,
            CreatedAt = role.CreatedAt,
            UpdatedAt = role.UpdatedAt,
        };
    }

}
