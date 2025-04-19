using RAppsAPI.Models.MPM;
using System.Collections.Concurrent;

namespace RAppsAPI.Services
{
    public class MPMBackgroundRequestQueue: IMPMBackgroundRequestQueue
    {
        private readonly SemaphoreSlim _signal = new(0);
        private readonly ConcurrentQueue<MPMEditRequestDTO> _reqQueue = new();

        public void QueueBackgroundRequest(MPMEditRequestDTO editReq)
        {
            if (editReq == null) throw new ArgumentNullException(nameof(editReq));
            _reqQueue.Enqueue(editReq);
            _signal.Release();
        }


        public async Task<MPMEditRequestDTO> DequeueAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            _reqQueue.TryDequeue(out var editReq);            
            return editReq;
        }
    }
}
