using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Web.Data.Entities.Admin;

namespace Web.Data.Entities.Identity;

public class AppUser : IdentityUser
{   

    [Column(TypeName = "varchar(500)")]
    [Required]
    public string FullName { get; set; } = string.Empty;

    [Column(TypeName = "varchar(500)")]
    public string Avatar { get; set; } = string.Empty;

    public int? SectionId { get; set; }    
    public virtual Section Section { get; set; }

    public int? SiteId { get; set; }
    public virtual BelongSite BelongSite { get; set; } 

    public string Position { get; set; }

    public DateTime? DateOfBirth { get; set; }
    
    [Column(TypeName = "varchar(300)")]
    [Required]
    public string EmpId{ get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsOnline { get; set; } = true;

    public bool IsOperator { get; set; } = false;

    public virtual ICollection<AppUserRole> UserRoles { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }
    public DateTime HireDate {  get; set; } = DateTime.Now;
}
