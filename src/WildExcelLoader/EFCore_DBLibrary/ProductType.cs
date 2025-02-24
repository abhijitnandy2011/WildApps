using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EFCore_DBLibrary;

[PrimaryKey("VfileId", "ProductTypeId")]
[Table("ProductTypes", Schema = "mpm")]
public partial class ProductType
{
    [Key]
    [Column("VFileID")]
    public int VfileId { get; set; }

    [Key]
    [Column("ProductTypeID")]
    public int ProductTypeId { get; set; }

    [StringLength(128)]
    public string Name { get; set; } = null!;

    [Column("ProductID")]
    public int ProductId { get; set; }

    public int CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedDate { get; set; }

    public int? LastUpdatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LastUpdatedDate { get; set; }

    [Column("RStatus")]
    public byte Rstatus { get; set; }
}
