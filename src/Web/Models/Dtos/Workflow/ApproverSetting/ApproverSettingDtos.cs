using System.ComponentModel.DataAnnotations;

namespace Web.Models.Dtos.Workflow.ApproverSetting;

public class ApproverSettingListItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string DisplayTitle { get; set; } = string.Empty;
    public string FormCode { get; set; } = string.Empty;
    public string UserNames { get; set; } = string.Empty;
}

public class ApproverSettingUpsertDto
{
    [Required]
    public string Title { get; set; }

    [Required]
    public string DisplayTitle { get; set; }

    [Required]
    public string FormCode { get; set; }

    [Required]
    public string UserNames { get; set; }
}
