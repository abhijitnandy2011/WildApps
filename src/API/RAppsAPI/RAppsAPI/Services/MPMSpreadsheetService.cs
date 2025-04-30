using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using OfficeOpenXml;
using RAppsAPI.Data;
using RAppsAPI.ExcelUtils;
using RAppsAPI.Models.MPM;
using System;
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


        // TODO: Edit request has to be validated by controller for some imp rules
        // before putting in Queue. Structural changes cannot be mixed with value changes.
        // Both must come as independent requests. If there is a lock, the request must not
        // be enqueued.
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
                // Db code - uses scope per task as RDBContext is not thread safe
                // https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/#avoiding-dbcontext-threading-issues
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<RDBContext>();
                if (dbContext == null)
                {                    
                    throw new Exception("ProcessRequest: Failed to get dbContext even after using scope");
                }
                var fileId = req.FileId;
                if (!_dictWorkbooks.ContainsKey(fileId))
                {
                    // Workbook is not in dict,create it
                    // TODO: Capture whole wb snap here & keep it around for later diffing
                    var (retCode, message) = BuildWorkbookFromDB(req, dbContext);
                    if (retCode > 0)
                    {
                        // TODO: Failed to create workbook, put the request in failed queue
                        throw new Exception($"Failed to create workbook for request{req.ReqId}");
                    }
                }
                var p = _dictWorkbooks[req.FileId];
                // Workbook is built, now apply the edits in the req & calc()
                HashSet<string> diffSheets = new();
                ApplyEditsToWorkbook(req, p, out diffSheets);
                // Update the sheet jsons in the cache from wb only for the ones mentioned in 'read'
                // section of edit req. Mark such rows as 'temp'. This enables reads to get updated
                // data ASAP, even if its marked as 'temp' while the wb is being written to DB.
                UpdateCacheFromWorkbook(req, serviceProvider, p);
                // Update DB from wb - needs to be immediate as changes have to be saved.
                // NOTE: Formatting etc changes have to applied directly from request as those
                // wont be in the wb.
                UpdateDBFromWorkbook(req, p, dbContext, diffSheets);
                // Invalidate all cache entries for the wb as new wb now written to DB.
                // ALSO UPDATE COMPLETED EDIT REQUEST LIST
                // New entries made by read reqs will now fetch updated data from DB itself.
                // So no more 'temp' rows.
                InvalidateCacheEntriesForWorkbook();
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
                // Release workbook sem as now read reqs can rebuild the cache from db
            }
        }


        // ALSO UPDATE COMPLETED EDIT REQUEST LIST
        private void InvalidateCacheEntriesForWorkbook()
        {
            // ALSO UPDATE COMPLETED EDIT REQUEST LIST
            throw new NotImplementedException();
        }


        private (int, string) BuildWorkbookFromDB(MPMEditRequestDTO req, RDBContext dbContext)
        {           
            try
            {
                var wbTools = new WBTools();
                ExcelPackage p;
                wbTools.BuildWorkbookFromDB(req, dbContext, out p);
                var fileId = req.FileId;
                _dictWorkbooks[fileId] = p;                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Ex: {ex.InnerException.Message}\n Trace:{ex.StackTrace}");
                }
            }            
            return (0, "");
        }

        private (int, string) ApplyEditsToWorkbook(
            MPMEditRequestDTO req,
            ExcelPackage ep,
            out HashSet<string> diffSheets)
        {
            diffSheets = new();
            // Make copy of data in all sheets
            // TODO: It may be faster to just write all sheets instead of wasting time
            // making a copy and diffing.
            //MPMWorkbookInMem memWb;
            ExcelPackage epCopy;
            WBTools wbTools = new();
            wbTools.CloneExcelPackage(ep, out epCopy);

            // Apply the changes------------------------
            // Worbook level changes - STRUCTURAL CHANGE - should be independent operation

            // Added sheets - STRUCTURAL CHANGE - should be independent operation

            var editedSheets = req.EditedSheets;
            foreach (var editedSheet in editedSheets)
            {
                var sheet = ep.Workbook.Worksheets[editedSheet.SheetName];
                if (sheet == null)
                {
                    Console.WriteLine($"ApplyEditsToWorkbook: Failed to find sheet with name:{editedSheet.SheetName}");
                    continue;
                }
                // Sheet name change - STRUCTURAL CHANGE - should be independent operation
                // Structural changes should not allow value changes in same request as they
                // already trigger a lot of formula changes which have to saved. This has to 
                // be enforced in code too. Value changes must be ignored when doing structural changes.
                // Write these changes in the log as structural changes loudly so its clear & detect them 
                // explictly. If we mix structural and value changes, and they are not applied in the same
                // order as user applied them, we will get the wrong values/wb state. This can happen due to
                // editreqs not being sent or queued in the correct order very easily. So the front end
                // must send a struct change & wait till its confirmed, blocking the UI, before sending another.

                // Added Rows - should be independent operation

                // Removed Rows  - should be independent operation

                // Added columns  - should be independent operation

                // Removed columns  - should be independent operation

                // Edited Rows                
                var editedRows = editedSheet.EditedRows;
                foreach(var editedRow in editedRows)
                {
                    var rn = editedRow.RN;
                    var editedCells = editedRow.Cells;
                    foreach(var editedCell in editedCells)
                    {
                        var cn = editedCell.CN;
                        sheet.Cells[rn,cn].Value = editedCell.Value;
                        sheet.Cells[rn, cn].Formula = editedCell.Formula;
                        // TODO: For edited cells, put the comment, number format and style into DB only
                        // No need to put it in the in-mem wb as they are not required for calc()
                    }
                }

                // Edited tables - resizes due to row changes - should be independent op

                // Added tables - should be independent operation

                // Removed tables - should be independent operation
            }
            ep.Workbook.Calculate();
            // TODO: Save to check?
            //---------------------------------------

            // Compare data with copy made earlier to get list of sheets & tables
            // to be written to DB. Sheets with user edited cells and tables which are edited
            // by the user directly, always get copied to DB.
            // For other cells, only value is diffed to determine if the sheet needs copying
            // Assumption: Tables are not changed by formula eval.            
            // First add all the sheets & tables from the user edits, those HAVE to be written
            // TODO: Check if Concat() works.
            foreach (var sheet in req.EditedSheets.Concat(req.AddedSheets))
            {
                diffSheets.Add(sheet.SheetName);
            }           
            // Now add more via comparison
            wbTools.CompareWorkbooks(ep, epCopy, out diffSheets);
            //var p = _dictWorkbooks[fileId];
            /* var sheet1 = p.Workbook.Worksheets["Sheet1"];
             sheet1.Cells[1, 2].Value = sheet1.Cells[1, 2].Value + " TestChange";
             Console.WriteLine("ApplyEditsToWorkbook: {0}", sheet1.Cells[1, 2].Value);*/
            return (0, "");
        }

        async private Task<(int, string)> UpdateCacheFromWorkbook(
            MPMEditRequestDTO req, 
            IServiceProvider serviceProvider,
            ExcelPackage ep)
        {
            var buildCacheService = serviceProvider.GetRequiredService<IMPMBuildCacheService>();
            var readReq = new MPMReadRequestDTO
            {
                ReqId = req.ReqId,
                FileId = req.FileId,
                TestRunTime = req.TestRunTime,
                Sheets = req.ReadSheets,
            };
            // Build directly from wb for now - wb is still locked by mutex so no edits will happen
            int retCode = await buildCacheService.BuildFromData(readReq, Timeout.Infinite, serviceProvider, ep);
            return (0, "");
        }

        private (int, string) UpdateDBFromWorkbook(
            MPMEditRequestDTO req,
            ExcelPackage p, 
            RDBContext dbContext,
            HashSet<string> diffSheets)
        {
            // Update the DB, wb is still locked
            // TODO: Edited cells format, style, comment needs to be applied directly from request.
            // Those will not be put in the wb.

            // On completion of writing diffs to DB, mark editReq as complete in cache entry for the file
            // for this user. This will inform frontend to pull data.
            return (0, "");
        }
        
    }

    
}
