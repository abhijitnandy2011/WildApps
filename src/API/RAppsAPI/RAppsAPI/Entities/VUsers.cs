using Microsoft.AspNetCore.Identity;

namespace RAppsAPI.Entities
{
    public class VUsers
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;        
        public string Email { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; } = false;
        public string? EmailToken {  get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime? LastLoginDate { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid? LastUpdatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }        
        public byte RStatus { get; set; } = 1;
        public virtual Guid RoleId { get; set; }
        public virtual VRoles Role { get; set; } = null!;
    }
}
