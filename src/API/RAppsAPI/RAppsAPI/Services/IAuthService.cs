using RAppsAPI.Entities;
using RAppsAPI.Models;

namespace RAppsAPI.Services
{
    public interface IAuthService
    {
        Task<RAppUser?> RegisterAsync(RegisterUserDto request);
        Task<string?> LoginAsync(string username, string password);
    }
}
