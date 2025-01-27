using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RAppsAPI.Data;

[PrimaryKey("SheetId", "RowNum", "ColNum")]
[Table("Cells", Schema = "rsa")]
public partial class Cell
{
    [Key]
    [Column("SheetID")]
    public int SheetId { get; set; }

    [Key]
    public int RowNum { get; set; }

    [Key]
    public int ColNum { get; set; }

    [StringLength(512)]
    public string Value { get; set; } = null!;

    [StringLength(1024)]
    public string Formula { get; set; } = null!;

    [StringLength(128)]
    public string Format { get; set; } = null!;

    [StringLength(1024)]
    public string Style { get; set; } = null!;

    public int CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedDate { get; set; }

    public int? LastUpdatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LastUpdatedDate { get; set; }

    [Column("RStatus")]
    public byte Rstatus { get; set; }
}
