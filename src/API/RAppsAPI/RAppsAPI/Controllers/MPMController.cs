using Microsoft.AspNetCore.Mvc;
using RAppsAPI.Data;
using RAppsAPI.Models;
using RAppsAPI.Services;

namespace RAppsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MPMController(IMPMService mpmService) : Controller
    {        
        // Returns all product, product types and ranges as nested json
        [HttpGet("mfile/{fileId}")]
        public async Task<IActionResult> GetProductInfo(int fileId)
        {
            try
            {
                var productsList = await mpmService.GetProductInfo(fileId);
                return Json(productsList);                
            }
            catch (Exception ex)
            {
                // Log error
                return StatusCode(500, ex.Message);
            }
        }



        // Returns range information(range header only)
        [HttpGet("mfile/{fileId}/range/{rangeId}")]
        public async Task<IActionResult> GetRangeInfo(int fileId, int rangeId, int? fromSeries, int? toSeries)
        {
            try
            {
                var rangeInfo = await mpmService.GetRangeInfo(fileId, rangeId, fromSeries, toSeries);
                return Json(rangeInfo);
            }
            catch (Exception ex)
            {
                // Log error
                return StatusCode(500, ex.Message);
            }
        }


        

    }

     

}
