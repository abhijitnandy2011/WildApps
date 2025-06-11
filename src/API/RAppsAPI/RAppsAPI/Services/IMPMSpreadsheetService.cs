
using RAppsAPI.Models.MPM;

namespace RAppsAPI.Services
{
    public interface IMPMSpreadsheetService
    {
        public Task ProcessQueueCommand(MPMBGQCommand qCmd, IServiceProvider serviceProvider);

    }
}