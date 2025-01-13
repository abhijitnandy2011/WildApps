using RAppsAPI.Entities;
using RAppsAPI.Models;

namespace RAppsAPI.Services
{
    public class AuthService : IAuthService
    {
        public Task<string?> LoginAsync(string username, string password)
        {
            throw new NotImplementedException();
        }

        public Task<RAppUser?> RegisterAsync(RegisterUserDto request)
        {
            throw new NotImplementedException();
        }
    }
}
