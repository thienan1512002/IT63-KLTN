using Web.Models.Enums;

namespace UserModule.Models.ImportModels;

public class UserImportRow
{
    public int RowNumber { get; set; }
    public ImportAction Action { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string EmpId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string DefaultPassword { get; set; }
    public string BelongSite { get; set; }
    public string Department { get; set; }
    public string Section { get; set; }
    public string Position { get; set; }
    public string DateOfBirth { get; set; }
    public string PhoneNumber { get; set; }
}
