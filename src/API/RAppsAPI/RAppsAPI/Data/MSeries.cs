using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RAppsAPI.Data;

[PrimaryKey("VfileId", "SeriesId")]
[Table("MSeries", Schema = "mpm")]
public partial class MSeries
{
    [Key]
    [Column("VFileID")]
    public int VfileId { get; set; }

    [Key]
    [Column("SeriesID")]
    public int SeriesId { get; set; }

    [StringLength(128)]
    public string Name { get; set; } = null!;

    [Column("RangeID")]
    public int RangeId { get; set; }

    [Column("SheetID")]
    public int SheetId { get; set; }

    [Column("HeaderTableID")]
    public int HeaderTableId { get; set; }

    [Column("DetailTableID")]
    public int DetailTableId { get; set; }

    public short SeriesNum { get; set; }

    public int CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedDate { get; set; }

    public int? LastUpdatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LastUpdatedDate { get; set; }

    [Column("RStatus")]
    public byte Rstatus { get; set; }
}
