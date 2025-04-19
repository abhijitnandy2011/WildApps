using Microsoft.AspNetCore.Mvc;
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
            _reqQueue = queue;
            _mpmService = mpmService;
        }

        [HttpPost("editFile")]
        public IActionResult editFile([FromBody] MPMEditRequestDTO editDTO)
        {
            _reqQueue.QueueBackgroundRequest(editDTO);
            return Ok("File edit request noted");
        }

        [HttpPost("readFile")]
        public IActionResult readFile([FromBody] MPMReadRequestDTO readDTO)
        {
            //_reqQueue.QueueBackgroundRequest(editDTO);
            return Ok("File read request noted");
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
        [HttpGet("mfile/{fileId}/range/{rangeId}")]
        public async Task<IActionResult> GetRangeInfo(int fileId, int rangeId, int? fromSeries, int? toSeries)
        {
            try
            {
                var rangeInfo = await _mpmService.GetRangeInfo(fileId, rangeId, fromSeries, toSeries);
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
