using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RAppsAPI.Data;

[PrimaryKey("VfileId", "TableId")]
[Table("MTables", Schema = "mpm")]
public partial class MTable
{
    [Key]
    [Column("VFileID")]
    public int VfileId { get; set; }

    [Key]
    [Column("TableID")]
    public int TableId { get; set; }

    [StringLength(512)]
    public string Name { get; set; } = null!;

    public int NumRows { get; set; }

    public int NumCols { get; set; }

    public int StartRowNum { get; set; }

    public int StartColNum { get; set; }

    public int EndRowNum { get; set; }

    public int EndColNum { get; set; }

    [Column("RangeID")]
    public int? RangeId { get; set; }

    [Column("SeriesID")]
    public int? SeriesId { get; set; }

    [Column("SheetID")]
    public int SheetId { get; set; }

    public int TableType { get; set; }

    [StringLength(1024)]
    public string Style { get; set; } = null!;

    public bool HeaderRow { get; set; }

    public bool TotalRow { get; set; }

    public bool BandedRows { get; set; }

    public bool BandedColumns { get; set; }

    public bool FilterButton { get; set; }

    public int CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedDate { get; set; }

    public int? LastUpdatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LastUpdatedDate { get; set; }

    [Column("RStatus")]
    public byte Rstatus { get; set; }
}
