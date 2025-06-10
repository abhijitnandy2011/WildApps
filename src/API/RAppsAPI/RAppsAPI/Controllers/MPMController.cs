using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RAppsAPI.Models.MPM;
using RAppsAPI.Services;

namespace RAppsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MPMController : Controller
    {
        private readonly IMPMService _mpmService;       
        

        public MPMController(IMPMService mpmService)
        {
            _mpmService = mpmService;
        }

        //[Authorize]
        [HttpPost("editFile")]
        public IActionResult editFile([FromBody] MPMEditRequestDTO editDTO)
        {
            var userId = 0;     // TODO: Get correct userId
            var response = _mpmService.EditFile(editDTO, userId);
            return Json(response);            
            
        }


        // Return series detail data
        // TODO: Maybe merge this with GetRangeInfo() to get all data in demand 
        //[Authorize]
        [HttpPost("readFile")]
        public async Task<IActionResult> readFile([FromBody] MPMReadRequestDTO readDTO)
        {
            var userId = 0;     // TODO: Get correct userId
            var response = await _mpmService.GetFileRows(readDTO, userId);
            return Json(response);
        }


        // Returns all product, product types and ranges as nested json
        [Authorize]
        [HttpGet("mfile/{fileId}")]
        public async Task<IActionResult> GetProductInfo(int fileId)
        {
            try
            {
                var productsList = await _mpmService.GetProductInfo(fileId);
                return Json(productsList);                
            }
            catch (Exception ex)
            {
                // Log error
                return StatusCode(500, ex.Message);
            }
        }



        // Returns range information(range header only)
        //[HttpGet("mfile/{fileId}/range/{rangeId}")]
        //public async Task<IActionResult> GetRangeInfo(int fileId, int rangeId, int? fromSeries, int? toSeries)
        //{
        //    try
        //    {
        //        var rangeInfo = await _mpmService.GetRangeInfo(fileId, rangeId, fromSeries, toSeries);
        //        return Json(rangeInfo);
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log error
        //        return StatusCode(500, ex.Message);
        //    }
        //}


        

    }

     

}
