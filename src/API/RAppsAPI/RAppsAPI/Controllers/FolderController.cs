using Microsoft.AspNetCore.Mvc;
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
                var resp = await folderService.Create(subFolderName, attributes, parentFolderId, 1);
                return Json(resp);                
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ReadID(int id)
        {
            try
            {
                var objList = await folderService.Read(id);
                return Json(objList);
                //return Ok("Read folder using ID");
            }
            catch (Exception ex)
            {
                // Log error
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateID(
            int id,
            string name,
            string description,
            string attributes)
        {
            try
            {
                return Ok("Update folder using ID");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }


        [HttpDelete]
        public async Task<IActionResult> DeleteID(int id)
        {
            try
            {
                return Ok("Delete folder using ID");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
