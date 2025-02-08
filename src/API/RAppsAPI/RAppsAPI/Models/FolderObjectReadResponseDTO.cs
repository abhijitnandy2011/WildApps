namespace RAppsAPI.Models
{
    public class FolderObjectReadResponseDTO
    {
        public int Id { get; set; }
        public int Code { get; set; } = -1;
        public string Message { get; set; } = string.Empty;
        public List<FolderObjectDTO> FolderObjects { get; set; }
    }
}
