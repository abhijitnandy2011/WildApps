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
    }
}
