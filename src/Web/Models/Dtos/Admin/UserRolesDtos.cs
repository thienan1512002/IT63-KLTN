namespace Web.Models.Dtos.Admin;

public class RoleAssignmentItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool Assigned { get; set; }
}

public class RoleAssignmentGroupDto
{
    public string CategoryName { get; set; } = string.Empty;
    public int CategoryOrder { get; set; }
    public IReadOnlyList<RoleAssignmentItemDto> Roles { get; set; } = Array.Empty<RoleAssignmentItemDto>();
}

public class UserRolesDetailDto
{
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    /// <summary>Id of the SuperUser role if present in the database.</summary>
    public string SuperUserRoleId { get; set; }
    public IReadOnlyList<RoleAssignmentGroupDto> Groups { get; set; } = Array.Empty<RoleAssignmentGroupDto>();
}

public class UpdateUserRolesRequestDto
{
    public List<string> RoleIds { get; set; }
}
