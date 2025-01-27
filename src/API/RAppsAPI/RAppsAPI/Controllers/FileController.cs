using Microsoft.AspNetCore.Mvc;
using RAppsAPI.Services;

namespace RAppsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController(IFileService fileService) : Controller
    {

        [HttpPost("createUnderPath")]
        public async Task<IActionResult> CreateUnderPath(
            string path,
            string name,             
            string description,
            string attributes)
        {
            try
            {
                return Ok("Create file under path");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("createUnderID")]
        public async Task<IActionResult> CreateUnderID(
            int id,
            string name,
            string description,
            string attributes)
        {
            try
            {
                return Ok("Create file under ID");
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
                return Ok("Read file using ID");
            }
            catch (Exception ex)
            {
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
                return Ok("Update file using ID");
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
                return Ok("Delete file using ID");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
