namespace Web.Models.Dtos.Admin;

public class UserListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string EmpId { get; set; } = string.Empty;
    public int? SectionId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public string SectionEnName { get; set; } = string.Empty;
    public string SectionViName { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int? BelongSiteId { get; set; }
    public string BelongSiteName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public bool IsActive { get; set; }
}

public class UserDetailVM : UserListItemDto
{
    public string Avatar { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public bool IsOnline { get; set; }

}