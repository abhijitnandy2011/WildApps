using RAppsAPI.Entities;
using RAppsAPI.Models;

namespace RAppsAPI.Services
{
    public interface IAuthService
    {
        Task<VUsers?> RegisterAsync(RegisterUserDto request);
        Task<string?> LoginAsync(string username, string password);
    }
}
