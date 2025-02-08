
using RAppsAPI.Models;

namespace RAppsAPI.Services
{
    public interface IFolderService
    {
        Task<FolderObjectReadResponseDTO> ReadUsingPath(string path);
        Task<FolderObjectReadResponseDTO> ReadUsingID(int parentFolderID);
        Task<FolderObjectUpdateResponseDTO> CreateUsingPath(string folderName, string attrs, string parentPath, int createdByUserName);
        Task<FolderObjectUpdateResponseDTO> CreateUsingID(string folderName, string attrs, int parentFolderID, int createdByUserName);

        Task<FolderObjectUpdateResponseDTO> UpdateUsingID(int folderID, string newName, string attrs, 
            string description, int modifiedByUserName);

        Task<FolderObjectUpdateResponseDTO> DeleteUsingID(int folderID, int deletedByUserID);
    }
}
