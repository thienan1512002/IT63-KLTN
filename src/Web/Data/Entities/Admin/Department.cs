using System.ComponentModel.DataAnnotations.Schema;
using Web.Data.Entities.Identity;

namespace Web.Data.Entities.Admin;

public class Department
{
    public int Id { get; set; }
    [Column(TypeName = "varchar(500)")]
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "varchar(300)")]
    public string ShortName { get; set; }

    [Column(TypeName = "varchar(2000)")]
    public string Description { get; set; }

    public int SiteId { get; set; }
    public virtual BelongSite BelongSite { get; set; }
    public bool IsActive { get; set; } = true;  


    [Column(TypeName = "varchar(255)")]
    public string MainApprover {get;set;}
    

    [Column(TypeName = "varchar(255)")]
    public string BackupApprover {get;set;}

    public virtual ICollection<Section> Sections { get; set; } = [];  

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    public DateTime? UpdatedAt { get; set; } = null!;

    [Column(TypeName = "varchar(100)")]
    public string CreatedBy { get; set; } = string.Empty;

    [Column(TypeName = "varchar(100)")]
    public string UpdatedBy { get; set; }
}
