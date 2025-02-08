namespace RAppsAPI.Models
{
    public class UpdateFolderRequestDTO
    {
        public int folderId { get; set; }
        public string folderName { get; set; } = string.Empty;

        public string description { get; set; } = string.Empty;
        public string attributes { get; set; } = string.Empty;        
        
    }
}
