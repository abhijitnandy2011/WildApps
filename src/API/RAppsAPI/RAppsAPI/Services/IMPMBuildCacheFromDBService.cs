// Builds cache entries from DB for only the required rows

using RAppsAPI.Models.MPM;

namespace RAppsAPI.Services
{
    public interface IMPMBuildCacheFromDBService
    {
        public Task<int> Build(MPMReadRequestDTO editReq, int waitTimeout, IServiceProvider serviceProvider);

    }
}
