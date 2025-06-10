
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml.Drawing;
using RAppsAPI.Models.MPM;
using System.Diagnostics;
using System.Drawing;


namespace RAppsAPI.Data
{
    public class RDBContext(DbContextOptions<RDBContext> options) : DbContext(options)
    {
        public virtual DbSet<AuthLog> AuthLogs { get; set; }

        public virtual DbSet<FileType> FileTypes { get; set; }

        public virtual DbSet<FileTypeApp> FileTypeApps { get; set; }

        public virtual DbSet<FileUpload> FileUploads { get; set; }

        public virtual DbSet<RappsRoot> RappsRoots { get; set; }

        public virtual DbSet<SysLog> SysLogs { get; set; }

        public virtual DbSet<SystemFolderFile> SystemFolderFiles { get; set; }

        public virtual DbSet<SystemUser> SystemUsers { get; set; }

        public virtual DbSet<VApp> VApps { get; set; }

        public virtual DbSet<VFile> VFiles { get; set; }

        public virtual DbSet<VFolder> VFolders { get; set; }

        public virtual DbSet<VRole> VRoles { get; set; }

        public virtual DbSet<VSystem> VSystems { get; set; }

        public virtual DbSet<VUser> VUsers { get; set; }

        // Car Mgr
        public virtual DbSet<Cell> Cells { get; set; }

        public virtual DbSet<MTable> Tables { get; set; }

        public virtual DbSet<Product> Products { get; set; }

        public virtual DbSet<ProductType> ProductTypes { get; set; }

        public virtual DbSet<MRange> Ranges { get; set; }

        public virtual DbSet<MSeries> Series { get; set; }

        public virtual DbSet<Sheet> Sheets { get; set; }

        public virtual DbSet<Edit> Edits { get; set; }

        public virtual DbSet<MPMAddWBEditResult> AddWBEditResults { get; set; }

        public virtual DbSet<MPMUpdateWBEditResult> UpdateWBEditResults { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {          
            modelBuilder.Entity<FileTypeApp>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK_FileTypeAppsID");
            });

            modelBuilder.Entity<FileUpload>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK_FileUploadsID");
            });

            modelBuilder.Entity<RappsRoot>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();
            });            

            modelBuilder.Entity<SystemFolderFile>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK_SystemFolderFilesID");
            });

            modelBuilder.Entity<SystemUser>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK_SystemUsersID");
            });

            modelBuilder.Entity<VFolder>(entity =>
            {
                entity.ToTable("VFolders", tb => tb.HasTrigger("trg_VFolders_PathDupeCheck"));
            });

            modelBuilder.Entity<VRole>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();
            });

            modelBuilder.Entity<VUser>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();
            });

            // MPM
            modelBuilder.Entity<Sheet>(entity =>
            {
                entity.HasKey(e => new { e.VfileId, e.SheetId }).HasName("PK_MPM_SheetsID");
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => new { e.VfileId, e.ProductId }).HasName("PK_MPM_ProductsID");
                entity.HasMany(e => e.ProductTypes);
            });

            modelBuilder.Entity<ProductType>(entity =>
            {
                entity.HasKey(e => new { e.VfileId, e.ProductTypeId }).HasName("PK_MPM_ProductTypesID");
                entity.HasOne(e => e.Product);
                entity.HasMany(e => e.MRanges);

            });

            modelBuilder.Entity<MRange>(entity =>
            {
                entity.HasKey(e => new { e.VfileId, e.RangeId }).HasName("PK_MPM_RangesID");
                entity.HasOne(e => e.ProductType);
            });

            modelBuilder.Entity<MSeries>(entity =>
            {
                entity.HasKey(e => new { e.VfileId, e.SeriesId }).HasName("PK_MPM_SeriesID");
            });

            modelBuilder.Entity<MTable>(entity =>
            {
                entity.HasKey(e => new { e.VfileId, e.TableId }).HasName("PK_MPM_MTablesID");
            });

            modelBuilder.Entity<Cell>(entity =>
            {
                entity.HasKey(e => new { e.VfileId, e.SheetId, e.RowNum, e.ColNum }).HasName("PK_MPM_CellsID");
            });

            modelBuilder.Entity<Edit>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK_MPM_EditID");
            });

            modelBuilder.Entity<MPMAddWBEditResult>().HasNoKey();

            modelBuilder.Entity<MPMUpdateWBEditResult>().HasNoKey();

        }



        public void logMsg(
            string module,
            int code,
            string msg,
            int createdByUserId,
            string description = "",
            int? objId1 = null,
            int? objId2 = null,
            int? objId3 = null)
        {
            Database.ExecuteSql($"EXECUTE dbo.logMsg {module}, {code}, {msg}, {description}, {createdByUserId}, {objId1}, {objId2}, {objId3}");
        }

        // TODO: Check this SP calling method & whether we are getting back values
        public MPMAddWBEditResult AddWBEdit(
            int userId,
            int fileId,
            string json,
            int trackingId,
            int code = 0,            
            string reason = "")
        {
            var result = AddWBEditResults.FromSql($"EXECUTE mpm.spAddWBEdit {userId}, {fileId}, {json}, {trackingId}, {code}, {reason}");
            var first = result.First();
            return first;
        }

        // TODO: Check this SP calling method & whether we are getting back values
        internal MPMUpdateWBEditResult UpdateWBEdit(
            int userId, 
            int fileId, 
            int editId, 
            int code = 0,
            string reason = "")
        {
            var result = UpdateWBEditResults.FromSql($"EXECUTE mpm.spUpdateWBEdit {userId}, {fileId}, {editId}, {code}, {reason}");
            var first = result.First();
            return first;
        }
    }


}
