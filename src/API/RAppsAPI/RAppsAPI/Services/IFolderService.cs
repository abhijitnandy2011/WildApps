using RAppsAPI.Data;
using RAppsAPI.Models;

namespace RAppsAPI.Services
{
    public interface IFolderService
    {
        Task<List<FolderObjectDTO>?> Read(string path);
        Task<List<FolderObjectDTO>?> Read(int parentFolderID);
        Task<FolderObjectDTO?> Create(string folderName, string path);
        Task<FolderObjectDTO> Create(string folderName, int parentFolderID);
    }
}
