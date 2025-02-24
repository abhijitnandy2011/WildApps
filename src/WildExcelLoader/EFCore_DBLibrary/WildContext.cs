
using EFCore_DBLibrary;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;


namespace EFCore_DBLibrary;

public partial class WildContext : DbContext
{
    public WildContext()
    {
    }

    public WildContext(DbContextOptions<WildContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Cell> Cells { get; set; }

    public virtual DbSet<MTable> Tables { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductType> ProductTypes { get; set; }

    public virtual DbSet<EFCore_DBLibrary.MRange> Ranges { get; set; }

    public virtual DbSet<MSeries> Series { get; set; }

    public virtual DbSet<Sheet> Sheets { get; set; }

    private static IConfigurationRoot _configuration;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true,
                reloadOnChange: true);
            _configuration = builder.Build();
            var cnstr = _configuration.GetConnectionString("WildDB");
            optionsBuilder.UseSqlServer(cnstr);
        }
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cell>(entity =>
        {
            entity.HasKey(e => new { e.VfileId, e.TableId, e.RowNum, e.ColNum }).HasName("PK_MPM_CellsID");
        });

        modelBuilder.Entity<MTable>(entity =>
        {
            entity.HasKey(e => new { e.VfileId, e.TableId }).HasName("PK_MPM_MTablesID");
        });

        modelBuilder.Entity<MRange>(entity =>
        {
            entity.HasKey(e => new { e.VfileId, e.RangeId }).HasName("PK_MPM_RangesID");
        });

        modelBuilder.Entity<MSeries>(entity =>
        {
            entity.HasKey(e => new { e.VfileId, e.SeriesId }).HasName("PK_MPM_SeriesID");
        });

        modelBuilder.Entity<Sheet>(entity =>
        {
            entity.HasKey(e => new { e.VfileId, e.SheetId }).HasName("PK_MPM_SheetsID");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
