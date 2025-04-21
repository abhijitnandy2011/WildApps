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
    public class MPMSpreadsheetService: IMPMSpreadsheetService
    {
        private readonly ConcurrentDictionary<int, SemaphoreSlim> _dictSemaphores = new();
        private readonly ConcurrentDictionary<int, ExcelPackage> _dictWorkbooks = new();


        public MPMSpreadsheetService(/*RDBContext context*/)
        {
            _dictSemaphores[1] = new SemaphoreSlim(1);
            _dictSemaphores[2] = new SemaphoreSlim(1);
            _dictSemaphores[3] = new SemaphoreSlim(1);
        }


        public async Task ProcessRequest(MPMEditRequestDTO req, IServiceProvider serviceProvider)
        {
            var semId = req.FileId;
            // TODO: check if semp exists with this id in dict or exception!
            var sem = _dictSemaphores[semId];
            try
            {
                Console.WriteLine("Request {0} waiting for lock...", req.ReqId);               
                await sem.WaitAsync();
                Console.WriteLine("Request {0} processing started!", req.ReqId);

                // Db code - 1 instance per task to prevent concurrency issues
                //using var scope = serviceProvider.CreateScope();
                //var dbContext = scope.ServiceProvider.GetRequiredService<RDBContext>();

                var fileId = req.FileId;
                if (!_dictWorkbooks.ContainsKey(fileId))
                {
                    // Workbook is not in dict,create it
                    // TODO: Capture whole wb snap here & keep it around for later diffing
                    var (retCode, message) = BuildWorkbookFromDB(fileId);
                    if (retCode > 0)
                    {
                        // TODO: Failed to create workbook, put the request in failed queue
                        throw new Exception($"Failed to create workbook for request{req.ReqId}");
                    }
                }

                // Workbook is built, now apply the edits in the req & calc()                
                ApplyEditsToWorkbook(fileId);
                // Update the sheet jsons in the cache from wb
                UpdateCacheFromWorkbook(fileId);
                // Update DB from wb - needs to be immediate as changes have to be saved.
                // Save only delta by diffing with before snap captured during wb build
                // TODO: We could make it every 2 mins later to batch saves. Let changes gather in
                // wb. There is a risk of losing changes though. It could be done by queing a request 
                // too. Use the same queue, but it has no edits, just a file operation to save the wb.
                // TODO: Write JSON model needs to have file operations section too, for adding/deleting
                // sheets etc.
                UpdateDBFromWorkbook();

                // Test code
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

            // Release workbook sem as now we can indep build the cache from db without the wb.
            try
            {
                // Update cache from DB
                // Refresh cache from db marking rows as from 'db' or front will keep trying
                // for some time & then throw error assuming file change was not saved.
                var buildCacheService = serviceProvider.GetRequiredService<IMPMBuildCacheFromDBService>();
                var readReq = new MPMReadRequestDTO{
                    ReqId = req.ReqId,
                    FileId = req.FileId,
                    TestRunTime = req.TestRunTime,
                    Sheets = req.ReadSheets,                    
                };
                buildCacheService.Build(readReq, Timeout.Infinite, serviceProvider);
            }
            catch (Exception ex)
            {
                // TODO: Log error
                string exMsg = ex.Message;
            }

        }


        private (int, string) BuildWorkbookFromDB(int fileId)
        {
            var p = new ExcelPackage();
            var sheet1 = p.Workbook.Worksheets.Add("Sheet1");
            sheet1.Cells[1, 1].Value = "ID";
            sheet1.Cells[1, 2].Value = "Product";
            _dictWorkbooks[fileId] = p;
            Console.WriteLine("BuildWorkbookFromDB: {0}", sheet1.Cells[1, 2].Value);
            return (0, "");
        }

        private (int, string) ApplyEditsToWorkbook(int fileId)
        {
            var p = _dictWorkbooks[fileId];
            var sheet1 = p.Workbook.Worksheets["Sheet1"];
            sheet1.Cells[1, 2].Value = sheet1.Cells[1, 2].Value + " TestChange";
            Console.WriteLine("ApplyEditsToWorkbook: {0}", sheet1.Cells[1, 2].Value);
            return (0, "");
        }

        private (int, string) UpdateCacheFromWorkbook(int fileId)
        {
            var wb = _dictWorkbooks[fileId];
            //var sheet1 = p.Workbook.Worksheets["Sheet1"];
            return (0, "");
        }

        private (int, string) UpdateDBFromWorkbook(/*int fileId*/)
        {
            //var wb = _dictWorkbooks[fileId];
            return (0, "");
        }
        
    }

    
}
