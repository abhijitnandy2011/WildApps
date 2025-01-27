using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Metadata;
using Microsoft.EntityFrameworkCore;

namespace RAppsAPI.Data;

public partial class SystemFolderFile
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Column("VSystemID")]
    public virtual int VSystemId { get; set; }

    [Column("VFileID")]
    public virtual int? VFileId { get; set; }

    [Column("VFolderID")]
    [ForeignKey(nameof(Folder))]
    public virtual int? VFolderId { get; set; }

    public bool Link { get; set; }

    [Column("VParentFolderID")]
    [ForeignKey(nameof(ParentFolder))]
    public virtual int VParentFolderId { get; set; }

    [ForeignKey(nameof(CheckedOutByUser))]
    public virtual int? CheckedOutBy { get; set; }

    public virtual int CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedDate { get; set; }

    public virtual int? LastUpdatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LastUpdatedDate { get; set; }

    [Column("RStatus")]
    public byte Rstatus { get; set; }

    public virtual VFile? File { get; set; }
    public virtual VFolder? Folder { get; set; }
    public virtual VFolder ParentFolder { get; set; } = null!;
    public virtual VUser? CheckedOutByUser { get; set; }
}
