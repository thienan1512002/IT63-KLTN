    using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Web.Data.Entities.Admin
{
    public class BelongSite
    {
        public int Id { get; set; }

        [Column(TypeName = "varchar(255)")]
        [Required]
        public string Name   { get; set; }

        [Column(TypeName = "varchar(255)")]
        public string Code { get; set; }

        [Column(TypeName = "varchar(2000)")]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}