using RAppsAPI.Models.MPM;

namespace RAppsAPI.Services
{
    public interface IMPMBackgroundRequestQueue
    {
        void QueueBackgroundRequest(MPMBGQCommand editReq);
        Task<MPMBGQCommand> DequeueAsync(CancellationToken cancellationToken);

    }
}
