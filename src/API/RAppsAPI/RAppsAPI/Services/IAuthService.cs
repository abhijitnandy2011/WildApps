using RAppsAPI.Data;
using RAppsAPI.Models;

namespace RAppsAPI.Services
{
    public interface IAuthService
    {
        //Task<VUser?> RegisterAsync(RegisterUserDto request);
        Task<LoginResponse> LoginAsync(string username, string password);
    }
}
