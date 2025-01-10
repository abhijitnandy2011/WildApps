using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using RAppsAPI.Entities;


namespace RAppsAPI.Data
{
    public class RDBContext: IdentityDbContext<RAppUser>
    {
        public RDBContext(DbContextOptions<RDBContext> options) : base(options) 
        { 
            
        }
    }
}
