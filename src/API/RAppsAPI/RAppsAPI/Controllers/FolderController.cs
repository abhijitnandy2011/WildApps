using Microsoft.AspNetCore.Mvc;
using RAppsAPI.Data;
using RAppsAPI.Models;
using RAppsAPI.Services;

namespace RAppsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FolderController(IFolderService folderService) : Controller
    {
        [HttpPost("createUsingPath")]
        public async Task<IActionResult> CreateUsingPath(
            string path,
            string name,
            string description,
            string attributes)
        {
            try
            {
                return Ok("Create folder under path");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("createUsingID")]
        public async Task<IActionResult> CreateUsingID(
            [FromBody] CreateFolderRequestDTO req)
        {
            try
            {
                int parentFolderId = req.parentFolderId;
                string subFolderName = req.subFolderName;
                string attributes = req.attributes;
                var resp = await folderService.CreateUsingID(subFolderName, attributes, parentFolderId, DBConstants.ADMIN_USER_ID);
                return Json(resp);                
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ReadUsingID(int id)
        {
            try
            {
                var objList = await folderService.ReadUsingID(id);
                return Json(objList);
                //return Ok("Read folder using ID");
            }
            catch (Exception ex)
            {
                // Log error
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("UpdateUsingID")]
        public async Task<IActionResult> UpdateUsingID(
            [FromBody] UpdateFolderRequestDTO req)            
        {
            try
            {
                int folderID = req.folderId;
                string newName = req.folderName;
                string description = req.description;
                string attrs = req.attributes;
                var resp = await folderService.UpdateUsingID(folderID, newName, attrs, description, DBConstants.ADMIN_USER_ID);
                return Json(resp);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }


        [HttpDelete]
        public async Task<IActionResult> DeleteID(int folderId)
        {
            try
            {
                var resp = await folderService.DeleteUsingID(folderId, DBConstants.ADMIN_USER_ID);
                return Json(resp);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("upload"), DisableRequestSizeLimit]
        public async Task<IActionResult> UploadFile([FromForm]UploadFileDTO model)
        {
            if (model.File == null || model.File.Length == 0)
            {
                return BadRequest("Invalid file");
            }

            var folderName = Path.Combine("Resources", "Uploads");
            var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), folderName);
            if (!Directory.Exists(pathToSave))
            {
                Directory.CreateDirectory(pathToSave);
            }
            var fileName = model.File.FileName;
            var fullPath = Path.Combine(pathToSave, fileName);
            var dbPath = Path.Combine(folderName, fileName);
            if (System.IO.File.Exists(dbPath))
            {
                return BadRequest($"File exists");
            }
            using(var stream = new FileStream(fullPath, FileMode.Create))
            {
                model.File.CopyTo(stream);
            }
            return Ok(new { dbPath });
        }

        [HttpPost("uploadmany"), DisableRequestSizeLimit]
        public async Task<IActionResult> UploadManyFiles([FromForm] UploadManyFilesDTO model)
        {
            if (model.Files == null || model.Files.Count == 0)
            {
                return BadRequest("Invalid files");
            }
            // Save the files
            var response = new Dictionary<string, string>();
            foreach (var file in model.Files)
            {
                if(file == null || file.Length == 0)
                {
                    response.Add(file?.FileName ?? "file", "Invalid file");                    
                }
                else
                {
                    var folderName = Path.Combine("Resources", "Uploads");
                    var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), folderName);
                    if (!Directory.Exists(pathToSave))
                    {
                        Directory.CreateDirectory(pathToSave);
                    }
                    var fileName = file.FileName;
                    var fullPath = Path.Combine(pathToSave, fileName);
                    var dbPath = Path.Combine(folderName, fileName);
                    if (System.IO.File.Exists(dbPath))
                    {
                        response.Add(fileName, "File exists");
                    }
                    else
                    {
                        using (var memStream = new MemoryStream())
                        {
                            await file.CopyToAsync(memStream);
                            await System.IO.File.WriteAllBytesAsync(fullPath, memStream.ToArray());
                        }
                        response.Add(fileName, dbPath);
                    }                      
                    
                }
            }            
            return Ok(new { response });
        }
    }
}
