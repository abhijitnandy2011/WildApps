using RAppsAPI.Data;
using RAppsAPI.Models;

namespace RAppsAPI.Services
{
    public class FileService(RDBContext context) : IFileService
    {
        public Task<string?> LoginAsync(string username, string password)
        {
            throw new NotImplementedException();
        }

        public Task<VUser?> RegisterAsync(RegisterUserDto request)
        {
            throw new NotImplementedException();
        }
    }
}
