
using Microsoft.EntityFrameworkCore;


namespace RAppsAPI.Data
{
    public class RDBContext(DbContextOptions<RDBContext> options) : DbContext(options)
    {
        public virtual DbSet<AuthLog> AuthLogs { get; set; }

        public virtual DbSet<Cell> Cells { get; set; }

        public virtual DbSet<FileType> FileTypes { get; set; }

        public virtual DbSet<FileTypeApp> FileTypeApps { get; set; }

        public virtual DbSet<FileUpload> FileUploads { get; set; }

        public virtual DbSet<RappsRoot> RappsRoots { get; set; }

        public virtual DbSet<Sheet> Sheets { get; set; }

        public virtual DbSet<SysLog> SysLogs { get; set; }

        public virtual DbSet<SystemFolderFile> SystemFolderFiles { get; set; }

        public virtual DbSet<SystemUser> SystemUsers { get; set; }

        public virtual DbSet<VApp> VApps { get; set; }

        public virtual DbSet<VFile> VFiles { get; set; }

        public virtual DbSet<VFolder> VFolders { get; set; }

        public virtual DbSet<VRole> VRoles { get; set; }

        public virtual DbSet<VSystem> VSystems { get; set; }

        public virtual DbSet<VUser> VUsers { get; set; }

        public virtual DbSet<Workbook> Workbooks { get; set; }

        public virtual DbSet<XlTable> XlTables { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Cell>(entity =>
            {
                entity.HasKey(e => new { e.SheetId, e.RowNum, e.ColNum }).HasName("PK_RSA_CellID");
            });

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

            modelBuilder.Entity<Sheet>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK_RSA_SheetID");
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

            modelBuilder.Entity<Workbook>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK_RSA_WorkbooksID");
            });

            modelBuilder.Entity<XlTable>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK_RSA_XlTableID");
            });
            
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
    }


}
