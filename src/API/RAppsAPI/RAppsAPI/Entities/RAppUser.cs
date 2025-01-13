using Microsoft.AspNetCore.Identity;

namespace RAppsAPI.Entities
{
    public class RAppUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        
        //virtual public string Email { get; set; }
        
        public string Location { get; set; } = string.Empty;    
        public DateTime CreatedOn { get; set; }
        public int RStatus { get; set; } = 1;
    }
}
