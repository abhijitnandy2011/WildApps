
using RAppsAPI.Models.MPM;

namespace RAppsAPI.Services
{
    public interface IMPMService
    {
        Task<MPMGetProductInfoResponseDTO> GetProductInfo(int fileId);
        //Task<MPMGetRangeInfoResponseDTO> GetRangeInfo(int fileId, int rangeId, int? fromSeries, int? toSeries);
        public Task<MPMReadRequestResponseDTO> GetFileRows(MPMReadRequestDTO readDTO, int userId);
        public Task<MPMEditRequestResponseDTO> EditFile(MPMEditRequestDTO editDTO, int userId);
    }
}
