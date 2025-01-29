namespace RAppsAPI.Models
{
    public class FolderObjectDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public int ObjectType  { get; set; }
        public string Attributes { get; set; } = string.Empty;
        public string OpenUrl { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedDate { get; set; } = string.Empty;
        public string LastUpdatedBy { get; set; } = string.Empty;
        public string LastUpdatedDate { get; set; } = string.Empty;
        
    }
}
