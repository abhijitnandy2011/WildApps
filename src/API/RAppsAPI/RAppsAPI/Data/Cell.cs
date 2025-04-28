using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RAppsAPI.Data;

[PrimaryKey("VfileId", "SheetId", "CellId")]
[Table("Cells", Schema = "mpm")]
public partial class Cell
{
    [Key]
    [Column("VFileID")]
    public int VfileId { get; set; }

    [Key]
    [Column("SheetID")]
    public int SheetId { get; set; }

    [Key]
    [Column("CellID")]
    public int CellId { get; set; }

    public int RowNum { get; set; }

    public int ColNum { get; set; }

    [StringLength(512)]
    public string Value { get; set; } = null!;

    [StringLength(1024)]
    public string Formula { get; set; } = null!;

    [StringLength(128)]
    public string Format { get; set; } = null!;

    [StringLength(1024)]
    public string Style { get; set; } = null!;

    [StringLength(1024)]
    public string Comment { get; set; } = null!;

    public int CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedDate { get; set; }

    public int? LastUpdatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LastUpdatedDate { get; set; }

    [Column("RStatus")]
    public byte Rstatus { get; set; }
}
