using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RAppsAPI.Data;

[Table("VFolders")]
public partial class VFolder
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [StringLength(1024)]
    public string Name { get; set; } = null!;

    [StringLength(128)]
    public string Attrs { get; set; } = null!;

    [StringLength(800)]
    public string Path { get; set; } = null!;

    [StringLength(800)]
    public string ParentIds { get; set; } = null!;

    [ForeignKey(nameof(CreatedByUser))]
    public virtual int CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedDate { get; set; }

    [ForeignKey(nameof(LastUpdatedByUser))]
    public virtual int? LastUpdatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LastUpdatedDate { get; set; }

    [Column("RStatus")]
    public byte Rstatus { get; set; }

    public virtual VUser CreatedByUser { get; set; } = null!;
    public virtual VUser? LastUpdatedByUser { get; set; }
}
