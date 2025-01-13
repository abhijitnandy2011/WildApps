
using Microsoft.EntityFrameworkCore;
using RAppsAPI.Entities;


namespace RAppsAPI.Data
{
    public class RDBContext(DbContextOptions<RDBContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }       
      
    }
}
