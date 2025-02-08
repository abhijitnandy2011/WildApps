using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RAppsAPI.Data;

public partial class SysLog
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [StringLength(128)]
    public string Module { get; set; } = null!;

    public int Code { get; set; }

    [StringLength(512)]
    public string Msg { get; set; } = null!;

    public string Description { get; set; } = null!;

    [Column("ObjectID1")]
    public int? ObjectId1 { get; set; }

    [Column("ObjectID2")]
    public int? ObjectId2 { get; set; }

    [Column("ObjectID3")]
    public int? ObjectId3 { get; set; }

    public int CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedDate { get; set; }
}
