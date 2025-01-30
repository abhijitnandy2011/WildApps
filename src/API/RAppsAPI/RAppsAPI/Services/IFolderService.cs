
using RAppsAPI.Models;

namespace RAppsAPI.Services
{
    public interface IFolderService
    {
        Task<List<FolderObjectDTO>?> Read(string path);
        Task<List<FolderObjectDTO>?> Read(int parentFolderID);
        Task<FolderObjectUpdateResponseDTO> Create(string folderName, string attrs, string parentPath, int createdByUserName);
        Task<FolderObjectUpdateResponseDTO> Create(string folderName, string attrs, int parentFolderID, int createdByUserName);

        Task<FolderObjectUpdateResponseDTO> updateFolder(int folderID, string newName, string attrs, string modifiedByUserName);
    }
}
