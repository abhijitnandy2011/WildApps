using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RAppsAPI;

[Table("Edits", Schema = "mpm")]
public partial class Edit
{
    [Key]
    [Column("ID")]
    public long Id { get; set; }

    [Column("VFileID")]
    public int VfileId { get; set; }

    [Column("BackupID")]
    public int BackupId { get; set; }

    public string? Json { get; set; }

    [Column("TrackingID")]
    public int TrackingId { get; set; }

    public int Code { get; set; }

    [StringLength(2048)]
    public string? Reason { get; set; }

    public int CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedDate { get; set; }

    public int? LastUpdatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LastUpdatedDate { get; set; }

    [Column("RStatus")]
    public byte Rstatus { get; set; }
}
