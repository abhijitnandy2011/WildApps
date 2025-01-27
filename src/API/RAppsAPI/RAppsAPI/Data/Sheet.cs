using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RAppsAPI.Data;

[Table("Sheets", Schema = "rsa")]
public partial class Sheet
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Column("WorkbookID")]
    public int WorkbookId { get; set; }

    [StringLength(128)]
    public string Name { get; set; } = null!;

    public short SheetNum { get; set; }

    [StringLength(2048)]
    public string Style { get; set; } = null!;

    public int StartRowNum { get; set; }

    public int StartColNum { get; set; }

    public int EndRowNum { get; set; }

    public int EndColNum { get; set; }

    public int CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedDate { get; set; }

    public int? LastUpdatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LastUpdatedDate { get; set; }

    [Column("RStatus")]
    public byte Rstatus { get; set; }
}
