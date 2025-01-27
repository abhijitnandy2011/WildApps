using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RAppsAPI.Data;

[Table("VSystems")]
public partial class VSystem
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [StringLength(1024)]
    public string Name { get; set; } = null!;

    public int? AssignedTo { get; set; }

    [Column("RootFolderID")]
    public int RootFolderId { get; set; }

    public int CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedDate { get; set; }

    public int? LastUpdatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LastUpdatedDate { get; set; }

    [Column("RStatus")]
    public byte Rstatus { get; set; }
}
