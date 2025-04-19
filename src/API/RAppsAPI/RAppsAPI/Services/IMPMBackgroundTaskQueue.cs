using RAppsAPI.Models.MPM;

namespace RAppsAPI.Services
{
    public interface IMPMBackgroundRequestQueue
    {
        void QueueBackgroundRequest(MPMEditRequestDTO editReq);
        Task<MPMEditRequestDTO> DequeueAsync(CancellationToken cancellationToken);

    }
}
