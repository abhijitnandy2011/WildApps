using Microsoft.AspNetCore.Identity;

namespace RAppsAPI.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;        
        public string Email { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; } = false;
        public string EmailToken {  get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;    
        public DateTime CreatedOn { get; set; }
        public DateTime? LastLoginOn { get; set; }
        public int RStatus { get; set; } = 1;
        public virtual Guid RoleId { get; set; }
        public virtual Role Role { get; set; } = null!;
    }
}
