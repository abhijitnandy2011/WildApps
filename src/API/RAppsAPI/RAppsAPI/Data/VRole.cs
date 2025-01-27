using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace RAppsAPI.Data
{
    public class VRole
    {
        [Key]
        [Column("ID")]
        public Guid Id { get; set; }

        [StringLength(128)]
        public string Name { get; set; } = null!;

        [StringLength(128)]
        public string Description { get; set; } = null!;

        public int CreatedBy { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime CreatedDate { get; set; }

        public int? LastUpdatedBy { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime? LastUpdatedDate { get; set; }

        [Column("RStatus")]
        public byte Rstatus { get; set; }

        public virtual ICollection<VUser> Users { get; } = null!;
    }
}
