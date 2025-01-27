using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RAppsAPI.Data;

[Table("Workbooks", Schema = "rsa")]
public partial class Workbook
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Column("VFileID")]
    public int VfileId { get; set; }

    [StringLength(128)]
    public string Name { get; set; } = null!;

    public short LastOpenedSheet { get; set; }

    public int LastFocusCellRow { get; set; }

    public int LastFocusCellCol { get; set; }

    [StringLength(2048)]
    public string? Settings { get; set; }

    public int CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedDate { get; set; }

    public int? LastUpdatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LastUpdatedDate { get; set; }

    [Column("RStatus")]
    public byte Rstatus { get; set; }
}
