using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace RAppsAPI.Data
{
    public class VUser
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [StringLength(128)]
        public string UserName { get; set; } = null!;

        [StringLength(128)]
        public string FirstName { get; set; } = null!;

        [StringLength(128)]
        public string? LastName { get; set; }

        [StringLength(512)]
        public string FullName { get; set; } = null!;

        [StringLength(128)]
        public string Email { get; set; } = null!;

        public bool EmailConfirmed { get; set; }

        [StringLength(40)]
        public string? EmailToken { get; set; }

        [StringLength(512)]
        public string Location { get; set; } = null!;

        [Column("RoleID")]
        public virtual Guid RoleId { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime? LastLoginDate { get; set; }

        public int CreatedBy { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime CreatedDate { get; set; }

        public int? LastUpdatedBy { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime? LastUpdatedDate { get; set; }

        [Column("RStatus")]
        public byte RStatus { get; set; }

        public virtual VRole Role { get; set; } = null!;
    }
}
