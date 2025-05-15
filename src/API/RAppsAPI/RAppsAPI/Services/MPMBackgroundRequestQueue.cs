using RAppsAPI.Models.MPM;
using System.Collections.Concurrent;

namespace RAppsAPI.Services
{
    public class MPMBackgroundRequestQueue: IMPMBackgroundRequestQueue
    {
        private readonly SemaphoreSlim _signal = new(0);
        private readonly ConcurrentQueue<MPMBGQCommand> _reqQueue = new();

        public void QueueBackgroundRequest(MPMBGQCommand qCmd)
        {
            if (qCmd == null) throw new ArgumentNullException(nameof(qCmd));
            _reqQueue.Enqueue(qCmd);
            _signal.Release();
        }


        public async Task<MPMBGQCommand> DequeueAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            _reqQueue.TryDequeue(out var qCmd);            
            return qCmd;
        }
    }
}
