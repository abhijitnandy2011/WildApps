using RAppsAPI.Data;
using RAppsAPI.Models;

namespace RAppsAPI.Services
{
    public interface IFileService
    {
        Task<VUser?> RegisterAsync(RegisterUserDto request);
        Task<string?> LoginAsync(string username, string password);
    }
}
