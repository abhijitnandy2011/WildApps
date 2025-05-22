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
        public async Task ProcessRequest(MPMBGQCommand qCmd, IServiceProvider serviceProvider)
        {
            var userId = qCmd.UserId;
            var req = qCmd.EditReq;
            var semId = req.FileId;
            // TODO: check if semp exists with this id in dict or exception!
            var sem = _dictSemaphores[semId];
            try
            {
                Console.WriteLine($"ProcessRequest:({req.ReqId},{qCmd.UserId}):Waiting for lock...");               
                await sem.WaitAsync();
                Console.WriteLine($"ProcessRequest:({req.ReqId},{qCmd.UserId}):Request processing started!");
                // Db code - uses scope per task(transient) as RDBContext is not thread safe
                // https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/#avoiding-dbcontext-threading-issues
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<RDBContext>();
                if (dbContext == null)
                {                    
                    throw new Exception($"ProcessRequest:({req.ReqId},{qCmd.UserId}):Failed to get dbContext even after using scope");
                }
                var fileId = req.FileId;
                if (!_dictWorkbooks.ContainsKey(fileId))
                {
                    // Workbook is not in dict,create it                    
                    var (retCode, message) = BuildWorkbookFromDB(req, userId, dbContext);
                    if (retCode > 0)
                    {
                        // TODO: Failed to create workbook, put the request in failed queue & log it
                        throw new Exception($"ProcessRequest:({req.ReqId},{qCmd.UserId}):Failed to build workbook from DB");
                    }
                }
                var p = _dictWorkbooks[req.FileId];
                // Workbook is built, now apply the edits in the req & calc()
                HashSet<string> diffSheets = new();
                ApplyEditsToWorkbook(req, userId, p, out diffSheets);
                // Update the sheet jsons in the cache from wb only for the ones mentioned in 'read'
                // section of edit req. Mark such rows as 'temp'. This enables reads to get updated
                // data ASAP, even if its marked as 'temp', while the wb is being written to DB.
                UpdateCacheFromWorkbook(req, userId, serviceProvider, p);
                // Update DB from wb - needs to be immediate as changes have to be saved.
                UpdateDBFromWorkbook(req, userId, p, dbContext, diffSheets);
                // Invalidate all cache entries for the wb as new wb now written to DB.
                // ALSO UPDATE COMPLETED EDIT REQUEST LIST
                // New entries made by read reqs will now fetch updated data from DB itself & update to cache.
                // So no more 'temp' rows after this point.
                InvalidateCacheEntriesForWorkbook();
                Console.WriteLine($"ProcessRequest:({req.ReqId},{qCmd.UserId}):Request processing complete");
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


       
        private (int, string) BuildWorkbookFromDB(
            MPMEditRequestDTO req, 
            int userId, 
            RDBContext dbContext)
        {           
            try
            {
                var wbTools = new WBTools();
                ExcelPackage p;
                // TODO: Check return code
                var resTuple = wbTools.BuildWorkbookFromDB(req, userId, dbContext, out p);
                var fileId = req.FileId;
                _dictWorkbooks[fileId] = p;                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"BuildWorkbookFromDB:({req.ReqId},{userId}):{ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"BuildWorkbookFromDB:({req.ReqId},{userId}):{ex.InnerException.Message}\n Trace:{ex.StackTrace}");
                }
            }            
            return (0, "");
        }


        // Apply edits to in-mem workbook.
        // TODO: Some structural changes are left to edit.
        // Structural changes must block and cannot be mixed with other operations
        // as their order matters. They need to be confirmed complete by blocking the
        // app before the next edit.
        // TODO: Structural changes are not checked if they were requested independently but
        // should be.
        private (int, string) ApplyEditsToWorkbook(
            MPMEditRequestDTO req,
            int userId,
            ExcelPackage ep,
            out HashSet<string> diffSheets)
        {
            diffSheets = new();
            try
            {
                // TODO: Write edit request to DB first, state as 'Processing' with time.
                // Set 'AppliedUpon' column to current file version.
                
                // Make copy of data in all sheets
                // TODO: It may be faster to just write all sheets instead of wasting time
                // making a copy and diffing.
                // TODO: ANother way, do not clone for every request. Start with the file in DB
                // & update the   in-mem copy during calm times. Else do not update the copy, there will be 
                // some extra data found different as edits go to db but not the in-mem copy.
                // But the bulk of the data will be unchanged so it will be prevented from
                // unnecessarily writing to DB.
                ExcelPackage epCopy;
                WBTools wbTools = new();
                wbTools.CloneExcelPackage(ep, out epCopy);

                // Apply the changes------------------------
                // TODO: Confirm if there are any structural changes and that they do not 
                // include any other changes - if they so then refuse to do the edit

                // Worbook level changes - STRUCTURAL CHANGE - should be independent operation

                // Renamed sheets - Structural
                foreach (var editedSheet in req.EditedSheets)
                {
                    if (editedSheet.RenameSheet)
                    {
                        // TODO
                        //ep.Workbook.Worksheets.Move
                    }
                }

                // Added sheets - STRUCTURAL CHANGE
                foreach (var addedSheet in req.AddedSheets)
                {
                    ep.Workbook.Worksheets.Add(addedSheet.SheetName);
                    // TODO: Add in correct position
                }

                // Removed sheets - STRUCTURAL CHANGE
                foreach (var removedSheet in req.RemovedSheets)
                {
                    // TODO: Any sheet dependencies to be checked & warned to user before deletion?
                    ep.Workbook.Worksheets.Delete(removedSheet);
                }

                // Edited sheets
                foreach (var editedSheet in req.EditedSheets)
                {
                    var sheet = ep.Workbook.Worksheets[editedSheet.SheetName];
                    if (sheet == null)
                    {
                        Console.WriteLine($"ApplyEditsToWorkbook: Failed to find sheet with name:{editedSheet.SheetName}");
                        continue;
                    }
                    // Sheet name change should not be allowed by front.
                    // That will make it impossible to locate the sheet as no ids used                
                    // Structural changes should not allow value changes in same request as they
                    // already trigger a lot of formula changes which have to saved. This has to 
                    // be enforced in code too. Value changes must be ignored when doing structural changes.
                    // Write these changes in the log as structural changes loudly so its clear & detect them 
                    // explictly. If we mix structural and value changes, and they are not applied in the same
                    // order as user applied them, we will get the wrong values/wb state. This can happen due to
                    // editreqs not being sent or queued in the correct order very easily. So the front end
                    // must send a struct change & wait till its confirmed, blocking the UI, before sending another.

                    // Added columns  - should be independent operation

                    // Removed columns  - should be independent operation

                    // TODO - Edited column widths

                    // Added Rows - should be independent operation

                    // Removed Rows  - should be independent operation

                    // Edited Rows                
                    // TODO: Check if edited rows actually get applied
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
                    // TODO: Check if this works
                    wbTools.SaveExcelPackage(ep, "E:\\Code\\RApps\\output\\a1.xlsx");                    

                    // TODO: Edited tables  - may be independent op, does it affect formulas?
                    // Front will send table resizes after row add/remove.
                    // TODO CHECK: They may also happen auto after applying row/column add/remove in wb which 
                    // could end up out of sync with front.
                    // NOTE: Front must supply all properties for new table when making any struct change to it.
                    // NOTE: Table name cannot be changed by front.
                    // TODO: Check if edited tables actually get applied
                    var editedTables = editedSheet.EditedTables ;
                    foreach (var editedTable in editedTables)
                    {
                        var table = sheet.Tables[editedTable.TableName];
                        if (table == null)
                        {
                            Console.WriteLine($"ApplyEditsToWorkbook: ERROR: Edited table:{editedTable.TableName} not found in sheet:{sheet.Name}");
                            continue;
                        }
                        // Remove old table
                        // TODO: Check if this works
                        //ep.Save();
                        sheet.Tables.Delete(editedTable.TableName);
                        // Re-create as new table
                        var startRow = editedTable.StartRowNum;
                        var startCol = editedTable.StartColNum;
                        if (editedTable.HeaderRow)
                            startRow++;
                        var endRow = editedTable.EndRowNum;
                        var endCol = editedTable.EndColNum;
                        var dstRange = sheet.Cells[startRow, startCol, endRow, endCol];
                        var dstTable = sheet.Tables.Add(dstRange, editedTable.TableName);
                        dstTable.ShowHeader = editedTable.HeaderRow;
                        dstTable.ShowTotal = editedTable.TotalRow;
                        dstTable.ShowRowStripes = editedTable.BandedRows;
                        dstTable.ShowColumnStripes = editedTable.BandedColumns;
                        dstTable.ShowFilter = editedTable.FilterButton;
                        Console.WriteLine($"ApplyEditsToWorkbook: Created table:{editedTable.TableName}");
                        // TODO: Check if this works
                        //ep.Save();
                    }
                    // TODO: Check if this works
                    //ep.Save();

                    // Added tables - should be independent operation

                    // Removed tables - should be independent operation
                }
                ep.Workbook.Calculate();
                // TODO: Save to check?
                // ep.Save();
                // TODO: Update edit request in DB, set state as 'Done' with time

                //---------------------------------------

                // Compare data with in-mem copy to get list of sheets & tables
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
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Ex: {ex.InnerException.Message}\n Trace:{ex.StackTrace}");
                }
            }
            //var p = _dictWorkbooks[fileId];
            /* var sheet1 = p.Workbook.Worksheets["Sheet1"];
             sheet1.Cells[1, 2].Value = sheet1.Cells[1, 2].Value + " TestChange";
             Console.WriteLine("ApplyEditsToWorkbook: {0}", sheet1.Cells[1, 2].Value);*/
            return (0, "");
        }

        async private Task<(int, string)> UpdateCacheFromWorkbook(
            MPMEditRequestDTO req,
            int userId,
            IServiceProvider serviceProvider,
            ExcelPackage ep)
        {
            try 
            { 
                var buildCacheService = serviceProvider.GetRequiredService<IMPMBuildCacheService>();
                var readReq = new MPMReadRequestDTO
                {
                    ReqId = req.ReqId,
                    FileId = req.FileId,
                    TestRunTime = req.TestRunTime,
                    Sheets = req.ReadSheets,
                };
                // Build cache directly from wb for now - wb is still locked by mutex so no edits will happen.
                // This will also update the state of the Edit Req as 'Intermediate' after cache is set, so rows
                // will be immediately available for reads.
                // TODO: Tables need to be fully put in their sheet cache entries if updated,
                // even if the edit.read section did not request it
                int retCode = await buildCacheService.BuildFromExcelPackage(readReq, userId, Timeout.Infinite, serviceProvider, ep);
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

        private (int, string) UpdateDBFromWorkbook(
            MPMEditRequestDTO req,
            int userId,
            ExcelPackage ep, 
            RDBContext dbContext,
            HashSet<string> diffSheets)
        {
            // TODO: Clear the current workbook's data, file mentioned in req only
            //TRUNCATE TABLE mpm.Sheets WHERE VFileId = req.FileId
            //TRUNCATE TABLE mpm.Products
            //TRUNCATE TABLE mpm.ProductTypes
            //TRUNCATE TABLE mpm.MRanges
            //TRUNCATE TABLE mpm.MSeries
            //TRUNCATE TABLE mpm.MTables
            //TRUNCATE TABLE mpm.Cells

            // Update the DB, wb is still locked
            //WBTools wbTools = new();
            //wbTools.UpdateDBFromWorkbook(req.FileId, ep, diffSheets, dbContext);

            // On completion of writing diffs to DB, mark editReq as complete in cache entry for the file
            // for this user. This will inform frontend to pull data.
            return (0, "");
        }


        // ALSO UPDATE COMPLETED EDIT REQUEST LIST
        private void InvalidateCacheEntriesForWorkbook()
        {
            // ALSO UPDATE COMPLETED EDIT REQUEST LIST
            throw new NotImplementedException();
        }

    }

    
}
