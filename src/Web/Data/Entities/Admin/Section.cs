
using System.ComponentModel.DataAnnotations.Schema;
using Web.Data.Entities.Identity;

namespace  Web.Data.Entities.Admin;

public class Section
{    
    public int Id { get; set; }

    [Column(TypeName = "varchar(500)")]
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "varchar(500)")]
    public string EngName { get; set; } = string.Empty;

    [Column(TypeName = "varchar(2000)")]
    public string Note { get; set; } = string.Empty;

    [Column(TypeName = "varchar(200)")]
    public string ShortName { get; set; } = string.Empty;

    [Column(TypeName = "varchar(255)")]
    public string MainApprover {get;set;}

    [Column(TypeName = "varchar(255)")]
    public string BackupApprover {get;set;}

    [Column(TypeName = "varchar(1000)")]
    public string PIC {get;set;}

    [Column(TypeName = "varchar(1000)")]
    public string DeputyApprovers {get;set;}

    public bool IsActive { get; set; } = true;   

    public int DepartmentId { get; set; }
    public virtual Department Department { get; set; } = null!;

    public int SiteId { get; set; }
    public virtual BelongSite BelongSite { get; set; }

    public virtual ICollection<AppUser> Users { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    public DateTime? UpdatedAt { get; set; } = null!;

    [Column(TypeName = "varchar(100)")]
    public string CreatedBy { get; set; } = string.Empty;

    [Column(TypeName = "varchar(100)")]
    public string UpdatedBy { get; set; }
}
