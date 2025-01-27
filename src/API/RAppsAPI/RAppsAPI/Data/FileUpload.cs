using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RAppsAPI.Data;

public partial class FileUpload
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Column("VFileID")]
    public int VfileId { get; set; }

    [Column("VAppID")]
    public int VappId { get; set; }

    [StringLength(512)]
    public string FileName { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public int CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedDate { get; set; }

    public int? LastUpdatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LastUpdatedDate { get; set; }

    [Column("RStatus")]
    public byte Rstatus { get; set; }
}
