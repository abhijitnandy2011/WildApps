using RAppsAPI.Entities;
using RAppsAPI.Models;

namespace RAppsAPI.Services
{
    public interface IAuthService
    {
        Task<User?> RegisterAsync(RegisterUserDto request);
        Task<string?> LoginAsync(string username, string password);
    }
}
