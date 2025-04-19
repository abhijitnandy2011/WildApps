using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RAppsAPI.Data;
using RAppsAPI.Models.MPM;
using System.Collections.Concurrent;
using static RAppsAPI.Data.DBConstants;

namespace RAppsAPI.Services
{
    public class MPMSpreadsheetService: IMPMSpreadsheetService
    {
        private readonly ConcurrentDictionary<int, SemaphoreSlim> _dictSemaphores = new();


        public MPMSpreadsheetService(/*RDBContext context*/)
        {
            _dictSemaphores[1] = new SemaphoreSlim(1);
            _dictSemaphores[2] = new SemaphoreSlim(1);
            _dictSemaphores[3] = new SemaphoreSlim(1);
        }


        public async Task processRequest(MPMEditRequestDTO req)
        {
            var semId = req.FileId;
            // TODO: check if semp exists with this id in dict or exception!
            var sem = _dictSemaphores[semId];
            try
            {
                Console.WriteLine("Request {0} waiting for lock...", req.ReqId);               
                await sem.WaitAsync();
                Console.WriteLine("Request {0} processing started!", req.ReqId);
                Thread.Sleep(req.TestRunTime);
                Console.WriteLine("Request {0} processing complete", req.ReqId);
            }
            catch (Exception ex)
            {
                // TODO: Log error
                string exMsg = ex.Message;                
            }
            finally
            {
                sem.Release();
            }            
        }
        
    }

    
}
