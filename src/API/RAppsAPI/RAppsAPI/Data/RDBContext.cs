
using Microsoft.EntityFrameworkCore;
using RAppsAPI.Entities;


namespace RAppsAPI.Data
{
    public class RDBContext(DbContextOptions<RDBContext> options) : DbContext(options)
    {
        public DbSet<VUser> Users { get; set; }
        public DbSet<VRole> Roles { get; set; }       
      
    }
}
