using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RAppsAPI.Data;

[Table("VApps")]
public partial class VApp
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [StringLength(1024)]
    public string Name { get; set; } = null!;

    [StringLength(1024)]
    public string Description { get; set; } = null!;

    public int Owner { get; set; }

    [StringLength(1024)]
    public string Settings { get; set; } = null!;

    public int CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedDate { get; set; }

    public int? LastUpdatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LastUpdatedDate { get; set; }

    [Column("RStatus")]
    public byte Rstatus { get; set; }
}
