namespace RAppsAPI.Models
{
    public class FolderObjectUpdateResponseDTO
    {
        public int Id { get; set; }
        public int ObjectType { get; set; }
        public int Code { get; set; } = -1;
        public string Message { get; set; } = string.Empty;     
    }
}
