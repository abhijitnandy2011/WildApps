using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using RAppsAPI.Data;
using RAppsAPI.Models.MPM;
using System.Collections.Concurrent;
using System.ComponentModel;
using static RAppsAPI.Data.DBConstants;


namespace RAppsAPI.Services
{
    public class MPMBuildCacheFromDBService : IMPMBuildCacheFromDBService
    {
        private readonly ConcurrentDictionary<int, SemaphoreSlim> _dictSemaphores = new();


        public MPMBuildCacheFromDBService()
        {
            _dictSemaphores[1] = new SemaphoreSlim(1);
            _dictSemaphores[2] = new SemaphoreSlim(1);
            _dictSemaphores[3] = new SemaphoreSlim(1);
        }


        // Immediately puts an entry in cache to indicate cache is being built from wb
        // Time of startng cache build is also noted in the entry
        // Front can check it & return later or be blocked by sem if it tries to Build() again
        // The sem avoids the problem where 2 threads both miss the cache leading to both creating
        // the cache entry from DB unnecessarily. Only one is needed.
        // Second thread must check if the entry was created after it gets the lock to avoid double work.
        // The cache entry for the row will be marked as 'db' from 'building'.
        // Time of ending cache build is also noted.
        // TODO: Can pass the RDBContext instead of the serviceProvider
        // waitTimeout=0 allows frontend read req from controller to wait & return imm if no sem available
        // & chk back later again for the value. This allows it to recheck cache for 'building' state & 
        // not start building a dupe. It will not block the second req either but it can pass waitTimeout=-1
        // if it wants to block. It may want to block if it wants to build a diff row & thats not in cache or
        // not marked as 'building'. Then it has to wait for the sem/mutex.
        // A call from write req via bgservice task will want to block as it has done some edit in the db
        // which may not may not be picked up by current cache build from db. Also the current build may
        // be building diff rows.
        // NOTE: Initially both read & write reqs should block indefn even if double work to ensure reqd rows are
        // built. Build() will not build entire sheet anyway so it should be fast.
        public async Task<int> Build(MPMReadRequestDTO req, int waitTimeout, IServiceProvider serviceProvider)
        {
            var semId = req.FileId;
            // TODO: check if semp exists with this id in dict or exception!
            var sem = _dictSemaphores[semId];
            try
            {
                Console.WriteLine("BuildCacheFromDBService: Request {0} waiting for lock...", req.ReqId);               
                var success = await sem.WaitAsync(waitTimeout);
                if(!success)
                {
                    return -1;
                }
                Console.WriteLine("BuildCacheFromDBService: Building cache rows for request {0}...", req.ReqId);
                // Make new cache entry/update entry for sheets mentioned in req marking the
                // required rows as 'building'
                MakeOrUpdateCacheRowsState(req, 'building');

                // Update cache from DB
                // Refresh cache from db marking rows as from 'db' or front will keep trying
                // for some time & then throw error assuming file change was not saved.
                // Db code - 1 instance per task to prevent concurrency issues
                //using var scope = serviceProvider.CreateScope();
                //var dbContext = scope.ServiceProvider.GetRequiredService<RDBContext>();


                // Test code
                Thread.Sleep(req.TestRunTime);
                Console.WriteLine("BuildCacheFromDBService: Cache rows built for request {0}", req.ReqId);
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

            return 0;
        }

        /*public void MakeOrUpdateCacheRowsState(req, 'building')
        {

        }*/


    }

    
}
