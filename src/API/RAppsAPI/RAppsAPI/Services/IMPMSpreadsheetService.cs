
using RAppsAPI.Models.MPM;

namespace RAppsAPI.Services
{
    public interface IMPMSpreadsheetService
    {
        public Task ProcessRequest(MPMBGQCommand qCmd, IServiceProvider serviceProvider);

    }
}