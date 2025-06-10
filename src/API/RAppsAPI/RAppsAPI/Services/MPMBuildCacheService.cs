using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OfficeOpenXml;
using RAppsAPI.Data;
using RAppsAPI.Models;
using RAppsAPI.Models.MPM;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using static RAppsAPI.Data.DBConstants;
using RAppsAPI.ExcelUtils;
using OfficeOpenXml.Table;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;


namespace RAppsAPI.Services
{
    public class MPMBuildCacheService : IMPMBuildCacheService
    {
        // Cache locks: FileId vs Semaphore - all sheets of the file are locked till done
        // TODO: Check if a cache lock is needed as cache entries are threadsafe.
        // TODO: Maybe File+Sheet wise locks may be better
        // e.g. ConcurrentDictionary<int, ConcurrentDictionary<int, SemaphoreSlim>>
        private readonly ConcurrentDictionary<int, SemaphoreSlim> _dictSemaphores = new();


        public MPMBuildCacheService()
        {           
        }


        // TODO: Immediately puts an entry in cache to indicate cache is being built from wb
        // TODO: Time of starting cache build is also noted in the entry
        // Returns: 0 on sucess
        //         -1 Exception
        //         -2 Failed to get semaphore lock
        //         -3 Failed to get the cache
        // Front can check it & return later or be blocked-till-timeout by sem if it tries to Build() again
        // The sem avoids the problem where 2 threads both miss the cache leading to both creating
        // the cache entry from DB unnecessarily. Only one is needed.
        // Second thread must check if the entry was created after it gets the lock to avoid double work.
        // The cache entry for the row will be marked as 'db' from 'building' once its ready.
        // Time of ending last cache build can also be noted on a per row basis.
        // TODO: Can pass the RDBContext instead of the serviceProvider
        // waitTimeout=0 allows frontend read req from controller to wait & return imm if no sem available
        // & chk back later again for the value. This allows it to recheck cache for 'building' state & 
        // not start building a dupe. It will not block the second req either but it can pass waitTimeout=-1
        // if it wants to block. It may want to block if it wants to build a diff row & thats not in cache or
        // not marked as 'building'. Then it has to wait for the sem/mutex.
        // A call from write req via bgservice task will want to block as it has done some edit in the db
        // which may not may not be picked up by current cache-build-from-db. Also the current build may
        // be building diff rows.
        // Also, the bgservice could want to update the cache-from-wb to mark rows as temp. Those must block too
        // if the cache entry is being built. So we ideally would want per sheet sems as the cache entries are per
        // sheet. That allows for fine grained locking.
        // NOTE: Initially both read & write reqs should block indefn even if double work to ensure reqd rows are
        // built. Build() will not build entire sheet anyway so it should be fast.
        public async Task<(int, string)> BuildFromDB(
            MPMReadRequestDTO req, 
            int userId, 
            int waitTimeout, 
            IServiceProvider serviceProvider)
        {
            int retCode = 0;
            string retMsg = "";
            var semId = req.FileId;
            // Check if semp exists with this id in dict or exception!
            if (!_dictSemaphores.ContainsKey(semId))
            {
                // Create file semaphore if not available in dict
                _dictSemaphores[semId] = new SemaphoreSlim(1);
            }
            var sem = _dictSemaphores[semId];
            try
            {
                // Lock sem right away to prevent another request from reading the db & 
                // possibly creating the same sheet entry in cache duplicating the work.
                Console.WriteLine($"BuildFromDB:({userId},{req.ReqId}):Waiting for lock...");
                var success = await sem.WaitAsync(waitTimeout);
                if (!success)
                {
                    return (-2, "sem lock timeout"); ;  // sem lock timeout, caller can block again or come back later
                }
                // Check if rows already created in cache from DB, by a previous req
                var cache = serviceProvider.GetRequiredService<IMemoryCache>();
                if (cache == null)
                {
                    Console.WriteLine($"BuildFromDB: Cache not obtained");
                    return (-3, "Failed to get cache");
                }
                var exists = CheckIfRowsAlreadyExistInCache(req, userId, cache);
                if (exists)
                {
                    // We are done
                    // The rows in cache from DB are not neccesariy latest rows.
                    // Cache eviction will refresh it later anyway
                    return (0, "");
                }
                // TODO: Can release sem as we wont access the cache right now
                //sem.Release();
                // No they dont, so read from db & put in cache
                Console.WriteLine($"BuildFromDB:({userId},{req.ReqId}):Building cache rows for request...");
                // Update cache from DB
                // Refresh cache from db marking rows as from 'db' or front will keep trying
                // for some time & then throw error assuming file change was not saved.
                // Db code - 1 instance per task to prevent concurrency issues
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<RDBContext>();
                var vFileIdParam = new SqlParameter("vFileIdParam", req.FileId);
                var activeStatusParam = new SqlParameter("activeStatusParam", DBConstants.RStatus.Active);
                foreach (var sheet in req.Sheets)
                {
                    var sheetName = sheet.SheetName;
                    if (sheet.Rects.Count == 0)
                    {
                        Console.WriteLine($"BuildFromDB:({userId},{req.ReqId}):There are no Read Rects given for sheet:{sheetName}. Skipping reads.");
                        continue;
                    }                    
                    var sheetNameParam = new SqlParameter("sheetNameParam", sheetName);                    
                    var rect = sheet.Rects[0];          // Only 1 rect read for now in 1 request.
                    var minRow = new SqlParameter("minRow", rect.top);
                    var minCol = new SqlParameter("minCol", rect.left);
                    var maxCol = new SqlParameter("maxCol", rect.right);
                    var maxRow = new SqlParameter("maxRow", rect.bottom);
                    // Query the db - we will have to have hard coded queries for 2 or 3 rects at max
                    // This query has not yet been possible to add conditions to, dynamically. SP may work.
                    //var query = $"SELECT * FROM mpm.Cells WHERE {conditions} ORDER BY RowNum, CellNum";
                    var listCellsResult = await dbContext.Database.SqlQuery<Cell>(
                        @$"SELECT c.* FROM 
                              mpm.Cells AS c INNER JOIN mpm.Sheets as s ON s.SheetID = c.SheetID AND 
                                s.VFileID = c.VFileID AND s.RStatus = c.RStatus
                            WHERE 
                              (c.RowNum >= {minRow} AND c.RowNum <= {maxRow} AND c.ColNum >= {minCol} AND c.ColNum <= {maxCol}) 
                                AND s.Name={sheetNameParam} AND c.VFileID={vFileIdParam} AND c.RStatus={activeStatusParam}
                            ORDER BY RowNum, ColNum"
                        ).ToListAsync();
                    // Setup new cache entry
                    var sheetCacheEntry = new MPMSheetCacheEntry()
                    {
                        FileId = req.FileId,
                        SheetName = sheetName,
                        EmptyRows = new(),
                        RowNumberVsRowEntry = new(),
                        Tables = new(),
                    };
                    // Copy from cells to cache entry struct
                    var dict = sheetCacheEntry.RowNumberVsRowEntry;
                    HashSet<int> setNonEmptyRows = new();
                    foreach (var cell in listCellsResult)
                    {
                        var r = cell.RowNum;
                        var c = cell.ColNum;
                        if (!dict.ContainsKey(r))
                        {
                            dict[r] = new()
                            {
                                State = MPMCacheRowState.DB,
                                Row = new()
                                {
                                    RN = r,
                                    Cells = new()
                                }
                            };
                        }
                        var rowEntry = dict[r];        
                        rowEntry.Row.Cells.Add(new()
                        {
                            CN = c,
                            Value = cell.Value,
                            Formula = cell.Formula,
                            Format = cell.Format,
                            Style = cell.Style,
                            Comment = cell.Comment,
                        });
                        setNonEmptyRows.Add(r);
                    } // foreach (var cell...
                    // Update empty rows list in cache entry                    
                    for (var r = rect.top; r <= rect.bottom; r++)
                    {
                        if(!setNonEmptyRows.Contains(r))
                            sheetCacheEntry.EmptyRows.Add(r);
                    }
                    // Now get the table info
                    var listTablesResult = await dbContext.Database.SqlQuery<MTable>(
                        @$"SELECT t.* FROM 
                          mpm.MTables AS t INNER JOIN mpm.Sheets as s ON s.SheetID = t.SheetID AND
                            s.VFileID = t.VFileID AND s.RStatus = t.RStatus
                        WHERE s.Name={sheetNameParam} AND t.VFileID={vFileIdParam} AND t.RStatus={activeStatusParam}"
                    ).ToListAsync();
                    foreach(var table in listTablesResult)
                    {
                        sheetCacheEntry.Tables.Add(new()
                        {
                            TableName = table.Name,
                            NumRows = table.NumRows,
                            NumCols = table.NumCols,
                            StartRowNum = table.StartRowNum,
                            StartColNum = table.StartColNum,
                            EndRowNum = table.EndRowNum,
                            EndColNum = table.EndColNum,
                            TableType = table.TableType,
                            Style = table.Style,
                            HeaderRow = table.HeaderRow,
                            TotalRow = table.TotalRow,
                            BandedRows = table.BandedRows,
                            BandedColumns = table.BandedColumns,
                            FilterButton = table.FilterButton,
                        });
                    }
                    // Update cache, mutex is locked already
                    var sheetCacheKey = Constants.GetWorkbookSheetCacheKey(req.FileId, sheetName);
                    var ttl = Constants.GetWorkbookSheetCacheEntryExpiration();
                    cache.Set(sheetCacheKey, sheetCacheEntry, ttl);
                    Console.WriteLine($"BuildFromDB:({userId},{req.ReqId}):Cache rows built for request: file:{req.FileId}, sheet:{sheetName}");
                } // foreach (var sheet ...
                Console.WriteLine($"BuildFromDB:({userId},{req.ReqId}):Cache rows completed for request");
            }
            catch (Exception ex)
            {
                // TODO: Log error
                retCode = -1;
                retMsg = ex.Message;
                Console.WriteLine($"BuildFromDB:({req.ReqId},{userId}):{ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"BuildFromDB:({req.ReqId},{userId}):{ex.InnerException.Message}\n Trace:{ex.StackTrace}");
                }

            }
            finally
            {
                sem.Release();
            }
            return (retCode, retMsg);
        }

        private bool CheckIfRowsAlreadyExistInCache(MPMReadRequestDTO req, int userId, IMemoryCache cache)
        {
            bool exist = true;
            foreach (var sheet in req.Sheets)
            {
                var sheetName = sheet.SheetName;
                if (sheet.Rects.Count == 0)
                {
                    Console.WriteLine($"CheckIfRowsAlreadyExistInCache:({userId},{req.ReqId}):There are no Read Rects given for sheet:{sheetName}. Skipping reads.");
                    continue;
                }                
                var cacheKey = Constants.GetWorkbookSheetCacheKey(req.FileId, sheetName);
                MPMSheetCacheEntry? entry;
                var success = cache.TryGetValue(cacheKey, out entry);
                if (!success || entry == null)
                {
                    Console.WriteLine($"CheckIfRowsAlreadyExistInCache:({userId},{req.ReqId}):Cache key not found, key:{cacheKey}");
                    exist = false;
                    break;
                }
                // Sheet cache entry is there, check if the required rows are there
                var dict = entry.RowNumberVsRowEntry;
                HashSet<int> setEmptyRows = new HashSet<int>(entry.EmptyRows);
                var rect = sheet.Rects[0];    // Only 1 rect read for now in 1 request.
                for (var r = rect.top; r <= rect.bottom; r++) 
                {
                    if (!setEmptyRows.Contains(r) && !dict.ContainsKey(r))
                    {
                        Console.WriteLine($"CheckIfRowsAlreadyExistInCache:({userId},{req.ReqId}):Row not found in cache, key:{cacheKey}, row:{r}");
                        exist = false;
                        break;
                    }
                }               
            }
            return exist;
        }


        // Returns: 0 on sucess
        //         -1 Exception
        //         -2 Failed to get semaphore lock
        //         -3 Failed to get the cache
        // Called by bgservice after an edit to create rows directly from wb. This is fast.
        // This will also update the state of the Edit Req as 'Intermediate' after cache is set.
        // The individual RowStates will be set to 'Temp' till they are refreshed from DB.
        // We get a lock on the cache right away as this function is called from another
        // thread(bgservice). This means a client request and a bg thread could be setting
        // the cache entry at the same time.
        public async Task<(int, string)> BuildFromExcelPackage(
            MPMReadRequestDTO req,
            int userId,
            int waitTimeout,
            IServiceProvider serviceProvider,
            ExcelPackage ep)
        {
            int retCode = 0;
            string retMsg = "";
            SemaphoreSlim sem;
            var semId = req.FileId;
            // Check if semp exists with this id in dict
            if (!_dictSemaphores.ContainsKey(semId))
            {
                // Create file semaphore if not available in dict
                _dictSemaphores[semId] = new SemaphoreSlim(1);
            }
            sem = _dictSemaphores[semId];
            try
            {                
                // Lock cache sem right away
                Console.WriteLine($"BuildFromExcelPackage:({userId},{req.ReqId}):waiting for lock...");
                var success = await sem.WaitAsync(waitTimeout);
                if (!success)
                {
                    return (-2, "sem lock timeout");  // sem lock timeout, caller can block again or come back later
                }
                var wbTools = new WBTools();
                var cache = serviceProvider.GetRequiredService<IMemoryCache>();
                if (cache == null)
                {
                    Console.WriteLine($"BuildFromExcelPackage: Cache not obtained");
                    return (-3, "Failed to get cache");
                }
                foreach (var reqSheet in req.Sheets)
                {
                    var sheetName = reqSheet.SheetName;
                    
                    var epSheet = ep.Workbook.Worksheets[sheetName];
                    if (epSheet == null)
                    {
                        Console.WriteLine($"BuildFromExcelPackage: WARN: Read has requested sheet:{sheetName} but it was not found in the wb");
                        continue;
                    }
                    var sheetCacheKey = Constants.GetWorkbookSheetCacheKey(req.FileId, sheetName);
                    MPMSheetCacheEntry? sheetCacheEntry;
                    success = cache.TryGetValue(sheetCacheKey, out sheetCacheEntry);
                    if (!success || sheetCacheEntry == null)
                    {
                        Console.WriteLine($"BuildFromExcelPackage: Cache key not found, req:{req.ReqId}, key:{sheetCacheKey}");
                        // No cache entry for this sheet yet, so create new one
                        sheetCacheEntry = new()
                        {
                            FileId = req.FileId,
                            SheetName = sheetName,
                            EmptyRows = new(),
                            RowNumberVsRowEntry = new(),
                            Tables = new(),
                        };
                    }
                    // Get sheet info if Rects in the sheet have been provided in the req
                    if (reqSheet.Rects.Count == 0)
                    {
                        Console.WriteLine($"BuildFromExcelPackage:({userId},{req.ReqId}):There are no Read Rects given for sheet:{sheetName}. Skipping reads.");
                        continue;
                    }
                    else
                    {
                        // Sheet cache entry is now there
                        var dict = sheetCacheEntry.RowNumberVsRowEntry;
                        var rect = reqSheet.Rects[0];  // Only 1 rect supported at a time for now
                        int startCol = rect.left; // epSheet.Dimension.Start.Column;
                        int endCol = rect.right;  //epSheet.Dimension.End.Column;
                        if (endCol > Constants.MAX_COLS_READ_IN_SHEET)
                        {
                            Console.WriteLine($"BuildFromExcelPackage: ERROR Really big number of columns{endCol}, rounding to {Constants.MAX_COLS_READ_IN_SHEET} columns");
                            endCol = Constants.MAX_COLS_READ_IN_SHEET;
                        }
                        // Add rows to sheet cache entry from epSheet
                        for (var r = rect.top; r <= rect.bottom; r++)
                        {
                            MPMSheetCacheRowEntry rowEntry;
                            if (dict.ContainsKey(r))
                            {
                                rowEntry = dict[r];
                                rowEntry.Row.Cells.Clear(); // All cells will be added again
                            }
                            else
                            {
                                Console.WriteLine($"BuildFromExcelPackage: Row not found in cache, req:{req.ReqId}, key:{sheetCacheKey}, row:{r}");
                                rowEntry = new()
                                {
                                    State = MPMCacheRowState.Temp,
                                    Row = new()
                                    {
                                        RN = r,
                                        Cells = new()
                                    }
                                };
                            }
                            // Add cells to sheet cache entry for row from epSheet
                            for (var c = startCol; c <= endCol; ++c)
                            {
                                var cell = epSheet.Cells[r, c];
                                string cellValue, cellFormula, cellComment, cellFormat;
                                MPMCellStyle cellStyle;
                                wbTools.GetCellProperties(cell, out cellValue, out cellFormula, out cellComment, out cellFormat, out cellStyle);
                                if (wbTools.IsCellEmpty(cellValue, cellFormula, cellComment, cellFormat, cellStyle))   // TODO: The style may need more detailed checks
                                {
                                    // Skip cell as its values are empty
                                    // TODO: Style and formatting needs to be checked too
                                    continue;
                                }
                                var styleJson = wbTools.GetCellStyleAsJSONString(cellStyle);
                                rowEntry.Row.Cells.Add(new()
                                {
                                    CN = c,
                                    Value = cellValue,
                                    Formula = cellFormula,
                                    Format = cellFormat,
                                    Style = styleJson,
                                    Comment = cellComment,
                                });
                            }
                            // All cells added
                            if (rowEntry.Row.Cells.Count == 0)
                            {
                                // If no cells found, add to EmptyRows set to prevent
                                // controller from going hunting for the row in the DB.
                                sheetCacheEntry.EmptyRows.Add(r);
                            }
                            else
                            {
                                // Cells found, so add to rows dict
                                dict[r] = rowEntry;
                            }
                        }
                    }
                    // Now get the table info if asked
                    if (reqSheet.IncludeTableInfo)
                    {
                        var listTablesResult = epSheet.Tables;
                        foreach (var epTable in listTablesResult)
                        {
                            var addr = epTable.Address;
                            var numRows = addr.End.Row - addr.Start.Row + 1;
                            var numCols = addr.End.Column - addr.Start.Column + 1;
                            sheetCacheEntry.Tables.Add(new()
                            {
                                TableName = epTable.Name,
                                NumRows = numRows,
                                NumCols = numCols,
                                StartRowNum = addr.Start.Row,
                                StartColNum = addr.Start.Column,
                                EndRowNum = addr.End.Row,
                                EndColNum = addr.End.Column,
                                TableType = (int)wbTools.GetSheetTableType(epTable, addr.Start.Row, addr.Start.Column, numRows, numCols),
                                Style = epTable.TableStyle.ToString(),
                                HeaderRow = epTable.ShowHeader,
                                TotalRow = epTable.ShowTotal,
                                BandedRows = epTable.ShowRowStripes,
                                BandedColumns = epTable.ShowColumnStripes,
                                FilterButton = epTable.ShowFilter,
                            });
                        }
                    }
                    // Set the cache entry
                    var ttl = Constants.GetWorkbookSheetCacheEntryExpiration();
                    cache.Set(sheetCacheKey, sheetCacheEntry, ttl);
                    Console.WriteLine($"BuildFromExcelPackage: Cache rows built for request:{req.ReqId}, file:{req.FileId}, sheet:{sheetName}");
                } // foreach(sheet...
                // Update edit request as completed               
                var userEditsCacheKey = Constants.GetCompletedEditRequestsCacheKey(userId);
                MPMUserEditReqsCacheEntry? userEditsCacheEntry;
                success = cache.TryGetValue(userEditsCacheKey, out userEditsCacheEntry);
                if (!success || userEditsCacheEntry == null)
                {
                    Console.WriteLine($"BuildFromExcelPackage: Cache key not found, req:{req.ReqId}, key:{userEditsCacheKey}");
                    // No cache entry yet, so create new one
                    userEditsCacheEntry = new()
                    {
                        ReqIdVsState = new(),
                    };
                }
                userEditsCacheEntry.ReqIdVsState[req.ReqId] = (int)MPMUserEditReqState.Intermediate;
                var ttlEdits = Constants.GetWorkbookEditsCacheEntryExpiration();
                cache.Set(userEditsCacheKey, userEditsCacheEntry, ttlEdits);                
                Console.WriteLine($"BuildFromExcelPackage: Cache entry is INTERMEDIATE, cache rows in TEMP state for request {req.ReqId}");
            }
            catch (Exception ex)
            {
                // TODO: Log error
                retCode = -1;
                retMsg = ex.Message;
                Console.WriteLine($"BuildFromExcelPackage:({req.ReqId},{userId}):{ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"BuildFromExcelPackage:({req.ReqId},{userId}):{ex.InnerException.Message}\n Trace:{ex.StackTrace}");
                }

            }
            finally
            {
                sem.Release();
            }
            return (retCode, retMsg);
        }


        // Takes a list of edit requests and userIds and updates their state as failed in the
        // respective user's 'failed edits' list in cache. A read can include them in its response.
        // Returns: 0 on success
        //          1 Edit req already in cache, overwritten
        //         -1 Exception
        //         -2 sem lock timeout
        //         -3 Failed to get cache
        //
        public async Task<(int, string)> UpdateFailedEditsInCache(
            List<MPMFailedEditReqInfoInternal> failedReqs,
            int waitTimeout,
            IServiceProvider serviceProvider)
        {
            int retCode = 0;
            string retMsg = "";            
            try
            {                
                Console.WriteLine($"UpdateFailedEditsInCache: waiting for lock...");                
                var wbTools = new WBTools();
                var cache = serviceProvider.GetRequiredService<IMemoryCache>();
                if (cache == null)
                {
                    Console.WriteLine($"UpdateFailedEditsInCache: Cache not obtained");
                    return (-3, "Failed to get cache");
                }
                foreach (var failedReq in failedReqs)
                {                    
                    SemaphoreSlim sem;
                    var req = failedReq.Req;
                    var semId = req.FileId;
                    // Check if semp exists with this id in dict
                    if (!_dictSemaphores.ContainsKey(semId))
                    {
                        // Create file semaphore if not available in dict
                        _dictSemaphores[semId] = new SemaphoreSlim(1);
                    }
                    sem = _dictSemaphores[semId];
                    // Lock cache sem right away
                    var success = await sem.WaitAsync(waitTimeout);
                    if (!success)
                    {
                        retCode = -2;
                        retMsg = "sem lock timeout";  // sem lock timeout, caller can block again or come back later
                        break;
                    }
                    var userId = failedReq.UserId;
                    var userFailedEditsCacheKey = Constants.GetFailedEditRequestsCacheKey(userId);
                    MPMUserFailedEditReqsCacheEntry? userFailedEditsCacheEntry;
                    success = cache.TryGetValue(userFailedEditsCacheKey, out userFailedEditsCacheEntry);
                    if (!success || userFailedEditsCacheEntry == null)
                    {
                        Console.WriteLine($"UpdateFailedEditsInCache: Cache key not found, req:{req.ReqId}, key:{userFailedEditsCacheEntry}");
                        // No cache entry yet, so create new one
                        userFailedEditsCacheEntry = new()
                        {
                            ReqIdVsFailedEditInfo = new(),
                        };
                    }
                    if (userFailedEditsCacheEntry.ReqIdVsFailedEditInfo.ContainsKey(req.ReqId))
                    {
                        Console.WriteLine($"WARN: UpdateFailedEditsInCache: Edit req id:{req.ReqId}, userid:{userId}, key:{userFailedEditsCacheEntry} is already in failed edits list in cache");
                        retCode = 1;
                        retMsg = "Edit req already in cache";
                    }
                    userFailedEditsCacheEntry.ReqIdVsFailedEditInfo[req.ReqId] = new() 
                    {
                        ReqId = req.ReqId,
                        Code = failedReq.Code,
                        Message = failedReq.Message,
                    };
                    var ttlFailedEdits = Constants.GetWorkbookFailedEditsCacheEntryExpiration();
                    cache.Set(userFailedEditsCacheKey, userFailedEditsCacheEntry, ttlFailedEdits);
                    sem.Release();
                } // foreach
            }
            catch (Exception ex)
            {
                // TODO: Log error
                retCode = -1;
                retMsg = ex.Message;
                Console.WriteLine($"UpdateFailedEditsInCache:{ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"UpdateFailedEditsInCache:{ex.InnerException.Message}\n Trace:{ex.StackTrace}");
                }
            }
            return (retCode, retMsg);
        }



    }


}
