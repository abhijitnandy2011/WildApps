// Builds cache entries from DB for only the required rows

using RAppsAPI.Models.MPM;

namespace RAppsAPI.Services
{
    public interface IMPMBuildCacheService
    {
        public Task<int> BuildFromDB(MPMReadRequestDTO editReq, int waitTimeout, IServiceProvider serviceProvider);
        public Task<int> BuildFromData(MPMReadRequestDTO editReq, int waitTimeout, IServiceProvider serviceProvider);

    }
}
