using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RAppsAPI;

[Table("WBEventLogs", Schema = "mpm")]
public partial class WbeventLog
{
    [Key]
    [Column("ID")]
    public long Id { get; set; }

    [Column("VFileID")]
    public int VfileId { get; set; }

    [Column("EventTypeID")]
    public int EventTypeId { get; set; }

    [Column("BackupID")]
    public int BackupId { get; set; }

    [Column("ID1")]
    public long Id1 { get; set; }

    public int CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedDate { get; set; }

    public int? LastUpdatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LastUpdatedDate { get; set; }

    [Column("RStatus")]
    public byte Rstatus { get; set; }
}
