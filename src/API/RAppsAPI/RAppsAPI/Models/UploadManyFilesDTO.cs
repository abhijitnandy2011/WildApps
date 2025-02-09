namespace RAppsAPI.Models
{
    public class UploadManyFilesDTO
    {
        public List<IFormFile> Files {  get; set; }       
        public string Description { get; set; }
        
    }
}
