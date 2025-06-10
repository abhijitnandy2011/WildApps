// Builds cache entries from DB for only the required rows

using OfficeOpenXml;
using RAppsAPI.Models.MPM;

namespace RAppsAPI.Services
{
    public interface IMPMBuildCacheService
    {
        public Task<(int, string)> BuildFromDB(
            MPMReadRequestDTO editReq,
            int userId,
            int waitTimeout, 
            IServiceProvider serviceProvider);
        public Task<(int, string)> BuildFromExcelPackage(
            MPMReadRequestDTO editReq,
            int userId,
            int waitTimeout,
            IServiceProvider serviceProvider,
            ExcelPackage p);
        public Task<(int, string)> UpdateFailedEditsInCache(
            List<MPMFailedEditReqInfoInternal> failedReqs,
            int waitTimeout,
            IServiceProvider serviceProvider);
    }
}
