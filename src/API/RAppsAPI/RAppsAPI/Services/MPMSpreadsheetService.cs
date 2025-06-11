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
using System.Text.Json;
using static RAppsAPI.Data.DBConstants;


namespace RAppsAPI.Services
{
    public class MPMSpreadsheetService: IMPMSpreadsheetService
    {
        // FileId vs semaphores for locking for access to workbooks
        private readonly ConcurrentDictionary<int, SemaphoreSlim> _dictSemaphores = new();
        // FileId vs the actual workbooks
        private readonly ConcurrentDictionary<int, ExcelPackage> _dictWorkbooks = new();
        // FileId vs workbook meta info to manage various things about it(access via the semaphore only)
        private readonly ConcurrentDictionary<int, MPMWBMetaInfo> _dictWorkbookMetaInfo = new();



        public MPMSpreadsheetService(/*RDBContext context*/)
        {
            // TODO: What is semaphore is not there in the dict, when do we add it?
            /*_dictSemaphores[1] = new SemaphoreSlim(1);
            _dictSemaphores[2] = new SemaphoreSlim(1);
            _dictSemaphores[3] = new SemaphoreSlim(1);*/
        }


        // TODO: Edit request has to be validated by controller for some imp rules
        // before putting in Queue. Structural changes cannot be mixed with value changes.
        // Both must come as independent requests. If there is a lock, the request must not
        // be enqueued.
        public async Task ProcessQueueCommand(MPMBGQCommand qCmd, IServiceProvider serviceProvider)
        {
            try
            {
                switch (qCmd.Command)
                {
                    case BGQueueCmd.Edit:
                        await ProcessEditCommand(qCmd, serviceProvider);
                        break;
                    case BGQueueCmd.WriteFiles:
                        await ProcessWriteFilesCommand(qCmd, serviceProvider);
                        break;
                    default:
                        // TODO Log error
                        Console.WriteLine($"ProcessQueueCommand: ERROR Unknown cmd:{qCmd.Command}");
                        break;
                }
                
            }
            catch (Exception ex)
            {
                // TODO: Log error
                Console.WriteLine($"ProcessQueueCommand: ERROR ({qCmd.Command},{qCmd.UserId}):{ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"ProcessQueueCommand: ERROR ({qCmd.Command},{qCmd.UserId}):{ex.InnerException.Message}\n Trace:{ex.StackTrace}");
                }
            }           
        }


        private async Task ProcessWriteFilesCommand(MPMBGQCommand qCmd, IServiceProvider serviceProvider)
        {
            int retCode = 0;
            string retMsg = "";
            var userId = qCmd.UserId;
            var req = qCmd.EditReq;
            var semId = req.FileId;
            if (!_dictSemaphores.ContainsKey(semId))
            {
                // There is nothing to write
                // TODO Log this
                Console.WriteLine($"ProcessWriteFilesCommand:({req.ReqId},{qCmd.UserId}):No semaphore found, no file with id:'{req.FileId}' to write, returning");
                return;
            }
            // There is a sem, so there maybe a file
            var sem = _dictSemaphores[semId];
            try
            {
                Console.WriteLine($"ProcessWriteFilesCommand:({req.ReqId},{qCmd.UserId}):Waiting for lock...");
                await sem.WaitAsync();
                Console.WriteLine($"ProcessWriteFilesCommand:({req.ReqId},{qCmd.UserId}):Request processing started!");
                var fileId = req.FileId;
                if (!_dictWorkbooks.ContainsKey(fileId))
                {
                    // No file to write in dict - not an issue as file has not been loaded for edit yet
                    // TODO Remove this message later
                    // TODO Log this
                    Console.WriteLine($"ProcessWriteFilesCommand:({req.ReqId},{qCmd.UserId}):No file with id:'{req.FileId}' to write, returning");
                    return;
                }
                // There is a file, check if modified
                var wbMetaInfo = _dictWorkbookMetaInfo[fileId];
                if (!wbMetaInfo.IsModified)
                {
                    Console.WriteLine($"ProcessWriteFilesCommand:({req.ReqId},{qCmd.UserId}):File with id:'{req.FileId}' not modified, skipping write to DB");
                    return;
                }
                // File is modified, need to write to DB
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<RDBContext>();
                if (dbContext == null)
                {
                    throw new Exception($"ProcessWriteFilesCommand: ERROR ({req.ReqId},{qCmd.UserId}):Failed to get dbContext even after using scope");
                }
                var elapsedTimeSinceLastWriteInSecs = (DateTime.Now - wbMetaInfo.LastWriteTime).TotalSeconds;
                if (elapsedTimeSinceLastWriteInSecs > wbMetaInfo.WriteFrequencyInSeconds)
                {
                    // Workbook needs to be written
                    // Making this part of an edit after cache updated. Not pushing cmd
                    // into queue for this for now.
                    var p = _dictWorkbooks[fileId];
                    HashSet<string> diffSheets = new();
                    (retCode, retMsg) = UpdateDBFromWorkbook(req, userId, p, dbContext, diffSheets);
                    wbMetaInfo.IsModified = false;
                    wbMetaInfo.LastWriteTime = DateTime.Now;
                    if (retCode < 0)
                    {
                        // TODO Log error
                    }
                    // TODO: Clear cache entries so they refresh, though ttl will clear anyway?
                    // Invalidate all cache entries for the wb as new wb now written to DB.
                    // New entries made by read reqs will now fetch updated data from DB itself & update to cache.
                    // So no more 'temp' rows after this point.                                                
                    //InvalidateCacheEntriesForWorkbook();
                }
                // else no need to update db
            }
            catch (Exception ex)
            {
                // TODO: Log error
                Console.WriteLine($"ProcessWriteFilesCommand: ERROR ({req.ReqId},{userId}):{ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"ProcessWriteFilesCommand:ERROR ({req.ReqId},{userId}):{ex.InnerException.Message}\n Trace:{ex.StackTrace}");
                }
            }
            finally
            {
                sem.Release();
                // Release workbook sem as now read reqs can rebuild the cache from db
            }
        }


        // TODO: Edit request has to be validated by controller for some imp rules
        // before putting in Queue. Structural changes cannot be mixed with value changes.
        // Both must come as independent requests. If there is a lock, the request must not
        // be enqueued.
        private async Task ProcessEditCommand(MPMBGQCommand qCmd, IServiceProvider serviceProvider)
        {
            int retCode = 0;
            string retMsg = "";
            var userId = qCmd.UserId;
            var req = qCmd.EditReq;
            var semId = req.FileId;
            if (!_dictSemaphores.ContainsKey(semId))
            {
                _dictSemaphores[semId] = new SemaphoreSlim(1);
            }
            var sem = _dictSemaphores[semId];
            try
            {
                Console.WriteLine($"ProcessEditCommand:({req.ReqId},{qCmd.UserId}):Waiting for lock...");
                await sem.WaitAsync();
                Console.WriteLine($"ProcessEditCommand:({req.ReqId},{qCmd.UserId}):Request processing started!");
                // Db code - uses scope per task(transient) as RDBContext is not thread safe
                // https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/#avoiding-dbcontext-threading-issues
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<RDBContext>();
                if (dbContext == null)
                {
                    throw new Exception($"ProcessEditCommand: ERROR ({req.ReqId},{qCmd.UserId}):Failed to get dbContext even after using scope");
                }
                var fileId = req.FileId;
                if (!_dictWorkbooks.ContainsKey(fileId))
                {
                    // Workbook is not in dict,create it                    
                    (retCode, retMsg) = BuildWorkbookFromDB(req, userId, dbContext);
                    if (retCode < 0)
                    {
                        // TODO: Failed to create workbook, put the request in failed queue & log it
                        // Log the failure
                        await UpdateFailedEditRequestInCache(
                            new List<MPMFailedEditReqInfoInternal> {
                                new() { Code=-1, Message=retMsg, Req=req, UserId=userId }
                            },
                            serviceProvider);
                        throw new Exception($"ProcessEditCommand: ERROR ({req.ReqId},{qCmd.UserId}):Failed to build workbook from DB");
                    }
                    // Add workbook meta info
                    if (_dictWorkbookMetaInfo.ContainsKey(fileId))
                    {
                        Console.WriteLine($"ProcessEditCommand: WARN : ({req.ReqId},{qCmd.UserId}): File:{fileId}, there is already a wb meta info entry without wb entry");
                    }
                    // Overwrite if present
                    _dictWorkbookMetaInfo[fileId] = new()
                    {
                        WriteFrequencyInSeconds = 300, // TODO - get this from DB in above BuildWorkbookFromDB()
                        LastWriteTime = DateTime.Now,
                        IsModified = false
                    };
                                            
                }
                var p = _dictWorkbooks[fileId];
                // Workbook is built, now apply the edits in the req & calc()
                HashSet<string> diffSheets = new();
                // TODO: Check the return code for each function call
                (retCode, retMsg) = ApplyEditsToWorkbook(req, userId, dbContext, p, out diffSheets);
                if (retCode < 0)
                {
                    // TODO: Edit has failed, need to update cache failed edits list with reason & code
                    // Log the failure
                    await UpdateFailedEditRequestInCache(
                             new List<MPMFailedEditReqInfoInternal> {
                                new() { Code=-2, Message=retMsg, Req=req, UserId=userId }
                             },
                             serviceProvider);
                    throw new Exception($"ProcessEditCommand: ERROR ({req.ReqId},{qCmd.UserId}):Failed to apply edits to workbook");

                }
                // Update the sheet jsons in the cache from wb only for the ones mentioned in 'read'
                // section of edit req. Mark such rows as 'temp'. This enables reads to get updated
                // data ASAP, even if its marked as 'temp', while the wb is being written to DB.
                (retCode, retMsg) = await UpdateCacheFromWorkbook(req, userId, serviceProvider, p);
                if (retCode < 0)
                {
                    // TODO Log error
                }
                // Mark this edit req as complete in DB as cache has been updated(cache complete edits list
                // has also been updated)
                (retCode, retMsg) = UpdateEditRequestAsCompleteInDB(
                                                req, userId, qCmd.RegdEditId, dbContext);
                if (retCode < 0)
                {
                    // TODO Log error
                }
                // Check if wb should be written to DB
                if (!_dictWorkbookMetaInfo.ContainsKey(fileId))
                {
                    // New entry
                    _dictWorkbookMetaInfo[fileId] = new()
                    {
                        WriteFrequencyInSeconds = 300  // TODO: Get this from DB, each wb can have diff
                    };
                }
                else
                {
                    // Existing entry, so check
                    var wbMetaInfo = _dictWorkbookMetaInfo[fileId];
                    var elapsedTimeSinceLastWriteInSecs = (DateTime.Now - wbMetaInfo.LastWriteTime).TotalSeconds;
                    if (elapsedTimeSinceLastWriteInSecs > wbMetaInfo.WriteFrequencyInSeconds)
                    {
                        // Workbook needs to be written
                        // Making this part of an edit after cache updated. Not pushing cmd
                        // into queue for this for now.
                        (retCode, retMsg) = UpdateDBFromWorkbook(req, userId, p, dbContext, diffSheets);
                        wbMetaInfo.IsModified = false;
                        wbMetaInfo.LastWriteTime = DateTime.Now;
                        if (retCode < 0)
                        {
                            // TODO Log error
                        }
                        // TODO: Clear cache entries so they refresh, though ttl will clear anyway?
                        // Invalidate all cache entries for the wb as new wb now written to DB.
                        // New entries made by read reqs will now fetch updated data from DB itself & update to cache.
                        // So no more 'temp' rows after this point.                                                
                        //InvalidateCacheEntriesForWorkbook();
                    }
                    // else no need to update db
                }
                Console.WriteLine($"ProcessEditCommand:({req.ReqId},{qCmd.UserId}):Request processing complete");
            }
            catch (Exception ex)
            {
                // TODO: Log error
                Console.WriteLine($"ProcessEditCommand: ERROR ({req.ReqId},{userId}):{ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"ProcessEditCommand:ERROR ({req.ReqId},{userId}):{ex.InnerException.Message}\n Trace:{ex.StackTrace}");
                }
            }
            finally
            {
                sem.Release();
                // Release workbook sem as now read reqs can rebuild the cache from db
            }
        }


        // Update edit request as complete in DB
        // Returns: 0 on success
        //         -1 Exception/error
        //         Remaining codes come from SP directly
        private (int retCode, string retMsg) UpdateEditRequestAsCompleteInDB(
            MPMEditRequestDTO req, int userId, int regdEditId, RDBContext dbContext)
        {
            int retCode = 0;
            string retMsg = "";
            try
            {
                var result = dbContext.UpdateWBEdit(userId, req.FileId, regdEditId, 0);
                if (result.RetCode < 0)
                {
                    // TODO: Log error to app log with SP name & return code
                    retCode = result.RetCode;
                    retMsg = result.Message;
                }
            }
            catch (Exception ex)
            {
                retCode = -1;
                retMsg = ex.Message;
                Console.WriteLine($"UpdateEditRequestAsCompleteInDB:({req.ReqId},{userId}):{ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"BuildWorkbookFromDB:({req.ReqId},{userId}):{ex.InnerException.Message}\n Trace:{ex.StackTrace}");
                }
            }
            return (retCode, retMsg);
        }



        // Build the workbook from DB data
        // Returns: 0 on success
        //         -1 on error
        private (int, string) BuildWorkbookFromDB(
            MPMEditRequestDTO req, 
            int userId, 
            RDBContext dbContext)
        {
            int retCode = 0;
            string retMsg = "";
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
                retCode = -1;
                retMsg = ex.Message;
                Console.WriteLine($"BuildWorkbookFromDB:({req.ReqId},{userId}):{ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"BuildWorkbookFromDB:({req.ReqId},{userId}):{ex.InnerException.Message}\n Trace:{ex.StackTrace}");
                }                
            }            
            return (retCode, retMsg);
        }


        // Apply edits to in-mem workbook.
        // Returns: 0 on success
        //   -1 Failed to get lock while registering edit
        //   -2 Exception
        // TODO: Some structural changes are left to edit.
        // Structural changes must block and cannot be mixed with other operations
        // as their order matters. They need to be confirmed complete by blocking the
        // app before the next edit.
        // TODO: Structural changes are not checked if they were requested independently but
        // should be.
        private (int, string) ApplyEditsToWorkbook(
            MPMEditRequestDTO req,
            int userId,
            RDBContext dbContext,
            ExcelPackage ep,
            out HashSet<string> diffSheets)
        {
            int retCode = 0;
            string retMsg = "";
            diffSheets = new();
            try
            {
                // Mark wb as modified right away
                _dictWorkbookMetaInfo[req.FileId].IsModified = true;
                WBTools wbTools = new WBTools();
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
            }
            catch (Exception ex)
            {
                // TODO log error
                Console.WriteLine($"ApplyEditsToWorkbook: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"ApplyEditsToWorkbook: Ex: {ex.InnerException.Message}\n Trace:{ex.StackTrace}");
                }
            }
            return (retCode, retMsg);
        }


        async private Task<(int, string)> UpdateCacheFromWorkbook(
            MPMEditRequestDTO req,
            int userId,
            IServiceProvider serviceProvider,
            ExcelPackage ep)
        {
            int retCode = 0;
            string retMsg = "";
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
                (retCode, retMsg) = await buildCacheService.BuildFromExcelPackage(readReq, userId, Timeout.Infinite, serviceProvider, ep);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Ex: {ex.InnerException.Message}\n Trace:{ex.StackTrace}");
                }
            }
            return (retCode, retMsg);
        }

        private (int, string) UpdateDBFromWorkbook(
            MPMEditRequestDTO req,
            int userId,
            ExcelPackage ep, 
            RDBContext dbContext,
            HashSet<string> diffSheets)
        {
            int retCode = 0;
            string retMsg = "";  
            try
            {
                Console.WriteLine("UpdateDBFromWorkbook: Writing to workbook to DB...");
                // NOTE: This is disabled for testing for now. Need to backup file before
                // allowing changed wb to be written, possibly corrupting it.
                // TODO: Clear the current workbook's data, file mentioned in req only
                // NOTE: This should be done via SP as the internal DB tables should not be exposed
                 // to C#, they can change making hard coded queries here invalid.
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
            }
            catch (Exception ex)
            {                
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Ex: {ex.InnerException.Message}\n Trace:{ex.StackTrace}");
                }
            }
            return (retCode, retMsg);
        }


        // Update failed request in cache
        // There are fixed codes returned for specific edit req processing fails
        // Fail code: -1 Failed to build workbook
        //            -2 Failed to apply edits to workbook
        private async Task<(int, string)> UpdateFailedEditRequestInCache(
            List<MPMFailedEditReqInfoInternal> failedReqs,
            IServiceProvider serviceProvider)
        {
            int retCode = 0;
            string retMsg = "";
            try
            {
                var buildCacheService = serviceProvider.GetRequiredService<IMPMBuildCacheService>();                
                (retCode, retMsg) = await buildCacheService.UpdateFailedEditsInCache(failedReqs, Timeout.Infinite, serviceProvider);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Ex: {ex.InnerException.Message}\n Trace:{ex.StackTrace}");
                }
            }
            return (retCode, retMsg);

        }


        // Delete all sheet cache entries for a workbook so they are rebuilt from DB
        // ALSO UPDATE COMPLETED EDIT REQUEST LIST
        private void InvalidateCacheEntriesForWorkbook()
        {
            // ALSO UPDATE COMPLETED EDIT REQUEST LIST & FAILED EDIT REQ LIST
            throw new NotImplementedException();
        }

    }

    
}
