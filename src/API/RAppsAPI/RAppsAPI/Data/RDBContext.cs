
using Microsoft.EntityFrameworkCore;
using RAppsAPI.Entities;


namespace RAppsAPI.Data
{
    public class RDBContext(DbContextOptions<RDBContext> options) : DbContext(options)
    {
        public DbSet<VUsers> Users { get; set; }
        public DbSet<VRoles> Roles { get; set; }       
      
    }
}
