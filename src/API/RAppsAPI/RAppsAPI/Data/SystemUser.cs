using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RAppsAPI.Data;

public partial class SystemUser
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Column("VSystemID")]
    public int VsystemId { get; set; }

    [Column("VUserID")]
    public int VuserId { get; set; }

    [StringLength(1024)]
    public string Profile { get; set; } = null!;

    public int CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedDate { get; set; }

    public int? LastUpdatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LastUpdatedDate { get; set; }

    [Column("RStatus")]
    public byte Rstatus { get; set; }
}
