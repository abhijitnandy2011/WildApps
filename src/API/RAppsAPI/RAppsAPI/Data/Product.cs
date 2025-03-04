using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using RAppsAPI.Data;

namespace EFCore_DBLibrary;

[PrimaryKey("VfileId", "ProductId")]
[Table("Products", Schema = "mpm")]
public partial class Product
{
    [Key]
    [Column("VFileID")]
    public int VfileId { get; set; }

    [Key]
    [Column("ProductID")]
    public int ProductId { get; set; }

    [StringLength(128)]
    public string Name { get; set; } = null!;

    [Column("SheetID")]
    public int SheetId { get; set; }

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
    public virtual List<ProductType> ProductTypes { get; set; } = new List<ProductType>();
}
