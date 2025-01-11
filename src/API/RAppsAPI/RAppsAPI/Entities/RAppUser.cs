using Microsoft.AspNetCore.Identity;

namespace RAppsAPI.Entities
{
    public class RAppUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string? LastName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Location { get; set; }        
        public DateTime CreatedOn { get; set; }
        public int RStatus { get; set; }
    }
}
