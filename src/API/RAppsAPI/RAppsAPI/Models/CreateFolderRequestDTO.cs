namespace RAppsAPI.Models
{
    public class CreateFolderRequestDTO
    {
        public int parentFolderId { get; set; }
        public string subFolderName { get; set; } = string.Empty;
        public string attributes { get; set; } = string.Empty;        
        
    }
}
