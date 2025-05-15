using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using RAppsAPI.Data;
using RAppsAPI.Models;
using RAppsAPI.Models.MPM;
using RAppsAPI.Services;

namespace RAppsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MPMController : Controller
    {
        private readonly IMPMService _mpmService;        
        private readonly IMPMBackgroundRequestQueue _reqQueue;

        public MPMController(IMPMService mpmService,
            IMPMBackgroundRequestQueue queue)
        {
            _mpmService = mpmService;
            _reqQueue = queue;            
        }

        [HttpPost("editFile")]
        public IActionResult editFile([FromBody] MPMEditRequestDTO editDTO)
        {
            MPMBGQCommand qCmd = new()
            {
                UserId = 0,
                EditReq = editDTO,
            };
            _reqQueue.QueueBackgroundRequest(qCmd);
            return Ok("File edit request noted");
        }


        // Return series detail data
        // TODO: Maybe merge this with GetRangeInfo() to get all data in demand 
        [HttpPost("readFile")]
        public async Task<IActionResult> readFile([FromBody] MPMReadRequestDTO readDTO)
        {
            var userId = 0;
            var response = await _mpmService.GetFileRows(readDTO, userId);
            return Json(response);
        }


        // Returns all product, product types and ranges as nested json
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
