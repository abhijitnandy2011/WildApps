using RAppsAPI.Models.MPM;


namespace RAppsAPI.Services
{
    public class MPMQueuedReqProcessorBackgroundService: BackgroundService
    {
        private readonly IMPMBackgroundRequestQueue _reqQueue;
        private readonly ILogger _logger;
        private readonly IMPMSpreadsheetService _spreadsheetService;

        public MPMQueuedReqProcessorBackgroundService(IMPMBackgroundRequestQueue reqQueue,
            ILoggerFactory loggerFactory,
            IMPMSpreadsheetService spreadsheetService)
        {
            _reqQueue = reqQueue;
            _logger = loggerFactory.CreateLogger<MPMQueuedReqProcessorBackgroundService>();
            _spreadsheetService = spreadsheetService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("MPMQueuedReqProcessorBackgroundService starting...");
            while (!stoppingToken.IsCancellationRequested)
            {
                MPMEditRequestDTO req = await _reqQueue.DequeueAsync(stoppingToken);
                // Create a Task and start it
                Task task1 = Task.Run(async () => {
                    if (stoppingToken.IsCancellationRequested)
                    {
                        return;
                    }
                    try
                    {
                        Console.WriteLine("Starting on request:{0}...", req.ReqId);                       
                        //_logger.LogInformation("Job completed successfully.");
                        await _spreadsheetService.processRequest(req);
                        Console.WriteLine("Finished request:{0}", req.ReqId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error occurred while working on request.");
                    }
                });
            }
            Console.WriteLine("MPMQueuedReqProcessorBackgroundService stopping...");
        }
    }
}
