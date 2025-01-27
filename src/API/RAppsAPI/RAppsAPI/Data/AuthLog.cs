using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RAppsAPI.Data;

public partial class AuthLog
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [StringLength(128)]
    public string Module { get; set; } = null!;

    [StringLength(512)]
    public string ErrorMsg { get; set; } = null!;

    public string Description { get; set; } = null!;

    public int CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedDate { get; set; }
}
