
using RAppsAPI.Models.MPM;

namespace RAppsAPI.Services
{
    public interface IMPMSpreadsheetService
    {
        public Task processRequest(MPMEditRequestDTO editReq);

    }
}
