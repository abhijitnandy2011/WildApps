namespace RAppsAPI.Models
{
    public class UploadFileDTO
    {
        public IFormFile File {  get; set; }       
        public string Description { get; set; }
        //public string FileName { get; set; }
        //public string ContentType { get; set; }
        //public bool IsUploading { get; set; }
        
    }
}
