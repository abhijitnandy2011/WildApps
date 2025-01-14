using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;

namespace RAppsAPI.Entities
{
    public class Role
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public virtual ICollection<User> Users { get; } = null!;
    }
}
