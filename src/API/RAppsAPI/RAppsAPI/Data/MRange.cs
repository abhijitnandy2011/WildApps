using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using RAppsAPI.Data;

namespace EFCore_DBLibrary;

[PrimaryKey("VfileId", "RangeId")]
[Table("MRanges", Schema = "mpm")]
public partial class MRange
{
    [Key]
    [Column("VFileID")]
    public int VfileId { get; set; }

    [Key]
    [Column("RangeID")]
    public int RangeId { get; set; }

    [StringLength(128)]
    public string Name { get; set; } = null!;

    [Column("SheetID")]
    public int SheetId { get; set; }

    [Column("ProductID")]
    public int ProductId { get; set; }

    [Column("ProductTypeID")]
    public int ProductTypeId { get; set; }

    [Column("HeaderTableID")]
    public int HeaderTableId { get; set; }

    public short RangeNum { get; set; }

    public int CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedDate { get; set; }

    public int? LastUpdatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LastUpdatedDate { get; set; }

    [Column("RStatus")]
    public byte Rstatus { get; set; }

    [ForeignKey(nameof(VfileId))]
    public virtual VFile File { get; set; }
    
    [ForeignKey("VfileId, ProductTypeId")]
    public virtual ProductType ProductType { get; set; } 
    
}
