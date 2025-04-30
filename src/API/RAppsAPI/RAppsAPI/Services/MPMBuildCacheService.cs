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


namespace RAppsAPI.Services
{
    public class MPMBuildCacheService : IMPMBuildCacheService
    {
        // FileId vs Semaphore - all sheets of the file are locked till done
        // TODO: Maybe File+Sheet wise locks may be better
        // e.g. ConcurrentDictionary<int, ConcurrentDictionary<int, SemaphoreSlim>>
        private readonly ConcurrentDictionary<int, SemaphoreSlim> _dictSemaphores = new();


        public MPMBuildCacheService()
        {           
        }


        // Immediately puts an entry in cache to indicate cache is being built from wb
        // Time of startng cache build is also noted in the entry
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
        public async Task<int> BuildFromDB(MPMReadRequestDTO req, int waitTimeout, 
            IServiceProvider serviceProvider)
        {
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
                Console.WriteLine($"BuildFromDB: Req {req.ReqId} waiting for lock...");
                var success = await sem.WaitAsync(waitTimeout);
                if (!success)
                {
                    return -1;  // sem lock timeout, caller can block again or come back later
                }
                // Check if rows already created in cache by a previous req
                var cache = serviceProvider.GetRequiredService<IMemoryCache>();
                var exists = CheckIfRowsAlreadyExistInCache(req, cache);
                if (exists)
                {
                    // We are done
                    return 0;
                }
                // TODO: Can release sem as we wont access the cache right now
                //sem.Release();
                // No they dont, so read from db & put in cache
                Console.WriteLine($"BuildFromDB: Building cache rows for request {req.ReqId}...");
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
                    var sheetNameParam = new SqlParameter("sheetNameParam", sheetName);                    
                    // Only 1 rect read for now in 1 request.
                    var rect = sheet.Rects[0];
                    var minRow = new SqlParameter("minRow", rect.top);
                    var minCol = new SqlParameter("minCol", rect.left);
                    var maxCol = new SqlParameter("maxCol", rect.right);
                    var maxRow = new SqlParameter("maxRow", rect.bottom);
                    // Query the db - we will have to have hard coded queries for 2 or 3 rects at max
                    // This query has not yet been possible to add conditions to, dynamically. SP may work.
                    //var query = $"SELECT * FROM mpm.Cells WHERE {conditions} ORDER BY RowNum, CellNum";
                    var listCellsResult = await dbContext.Database.SqlQuery<Cell>(
                            @$"SELECT * FROM mpm.Cells WHERE 
                            (RowNum >= {minRow} AND RowNum <= {maxRow} AND ColNum >= {minCol} AND ColNum <= {maxCol}) 
                            AND Name={sheetNameParam} AND VFileID={vFileIdParam} AND RStatus={activeStatusParam}
                            ORDER BY RowNum, ColNum"
                        ).ToListAsync();
                    // Copy from cells to cache entry struct
                    var cacheEntry = new MPMSheetCacheEntry()
                    {
                        FileId = req.FileId,
                        SheetName = sheetName,
                        EmptyRows = new(),
                        RowNumberVsRowEntry = new()
                    };
                    var dict = cacheEntry.RowNumberVsRowEntry;
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
                            cacheEntry.EmptyRows.Add(r);
                    }
                    // Update cache, mutex is locked already
                    var cacheKey = "MPM_" + req.FileId + "_" + sheetName;
                    cache.Set(cacheKey, cacheEntry);
                    Console.WriteLine($"BuildFromDB: Cache rows built for request:{req.ReqId}, file:{req.FileId}, sheet:{sheetName}");
                } // foreach (var sheet ...
                Console.WriteLine($"BuildFromDB: Cache rows completed for request {req.ReqId}");
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

        private bool CheckIfRowsAlreadyExistInCache(MPMReadRequestDTO req, IMemoryCache cache)
        {
            bool exist = true;
            foreach (var sheet in req.Sheets)
            {
                var sheetName = sheet.SheetName;
                var cacheKey = "MPM_" + req.FileId + "_" + sheetName;
                MPMSheetCacheEntry? entry;
                var success = cache.TryGetValue(cacheKey, out entry);
                if (!success || entry == null)
                {
                    Console.WriteLine($"CheckIfRowsAlreadyExistInCache: Cache key not found, req:{req.ReqId}, key:{cacheKey}");
                    exist = false;
                    break;
                }
                // Sheet cache entry is there, check if the required rows are there
                var dict = entry.RowNumberVsRowEntry;
                HashSet<int> setEmptyRows = new HashSet<int>(entry.EmptyRows);
                var rect = sheet.Rects[0];
                for (var r = rect.top; r <= rect.bottom; r++) 
                {
                    if (!setEmptyRows.Contains(r) && !dict.ContainsKey(r))
                    {
                        Console.WriteLine($"CheckIfRowsAlreadyExistInCache: Row not found in cache, req:{req.ReqId}, key:{cacheKey}, row:{r}");
                        exist = false;
                        break;
                    }
                }               
            }
            return exist;
        }


        // Called by bgservice after an edit to create rows directly from wb. This is fast.
        // This will also update the state of the Edit Req as 'Intermediate' after cache is set.
        // We get a lock on the cache right away as this function is called from another
        // thread(bgservice). This means a client request and a bg thread could be setting
        // the cache entry at the same time.
        public async Task<int> BuildFromData(
            MPMReadRequestDTO req,
            int waitTimeout,
            IServiceProvider serviceProvider,
            ExcelPackage ep)
        {
            var semId = req.FileId;
            // Check if semp exists with this id in dict
            if (!_dictSemaphores.ContainsKey(semId))
            {
                // Create file semaphore if not available in dict
                _dictSemaphores[semId] = new SemaphoreSlim(1);
            }
            var sem = _dictSemaphores[semId];
            try
            {
                // Lock sem right away
                Console.WriteLine($"BuildFromData: Req {req.ReqId} waiting for lock...");
                var success = await sem.WaitAsync(waitTimeout);
                if (!success)
                {
                    return -1;  // sem lock timeout, caller can block again or come back later
                }
                var wbTools = new WBTools();
                var cache = serviceProvider.GetRequiredService<IMemoryCache>();
                if (cache == null)
                {
                    Console.WriteLine($"BuildFromData: Cache not obtained");
                    return 1;
                }
                foreach (var sheet in req.Sheets)
                {
                    var sheetName = sheet.SheetName;
                    var epSheet = ep.Workbook.Worksheets[sheetName];
                    if (epSheet == null)
                    {
                        Console.WriteLine($"BuildFromData: WARN: Read has requested sheet:{sheetName} but it was not found in the wb");
                        continue;
                    }
                    var cacheKey = "MPM_" + req.FileId + "_" + sheetName;
                    MPMSheetCacheEntry? cacheEntry;
                    success = cache.TryGetValue(cacheKey, out cacheEntry);
                    if (!success || cacheEntry == null)
                    {
                        Console.WriteLine($"BuildFromData: Cache key not found, req:{req.ReqId}, key:{cacheKey}");
                        // No cache entry for this sheet yet, so create new one
                        cacheEntry = new()
                        {
                            FileId = req.FileId,
                            SheetName = sheetName,
                            EmptyRows = new(),
                            RowNumberVsRowEntry = new(),
                            Tables = new(),
                        };                        
                    }
                    // Sheet cache entry is now there
                    var dict = cacheEntry.RowNumberVsRowEntry;
                    var rect = sheet.Rects[0];  // Only 1 rect supported at a time for now
                    int startCol = epSheet.Dimension.Start.Column;
                    int endCol = epSheet.Dimension.End.Column;
                    if (endCol > Constants.MAX_COLS_READ_IN_SHEET)
                    {
                        Console.WriteLine($"BuildFromData: ERROR Really big number of columns{endCol}, rounding to {Constants.MAX_COLS_READ_IN_SHEET} columns");
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
                            Console.WriteLine($"BuildFromData: Row not found in cache, req:{req.ReqId}, key:{cacheKey}, row:{r}");
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
                            cacheEntry.EmptyRows.Add(r);
                        }
                        else
                        {
                            // Cells found, so add to rows dict
                            dict[r] = rowEntry;
                        }
                    }
                    // Set the cache entry
                    cache.Set(cacheKey, cacheEntry);
                    Console.WriteLine($"BuildFromData: Cache rows built for request:{req.ReqId}, file:{req.FileId}, sheet:{sheetName}");
                } // foreach(sheet...
                // Update request as completed
                var userId = 1; // TODO: Put proper user id
                var userEditsCacheKey = $"MPM_EditReqList_{userId}";
                MPMUserEditReqsCacheEntry? userEditsCacheEntry;
                success = cache.TryGetValue(userEditsCacheKey, out userEditsCacheEntry);
                if (!success || userEditsCacheEntry == null)
                {
                    Console.WriteLine($"BuildFromData: Cache key not found, req:{req.ReqId}, key:{userEditsCacheKey}");
                    // No cache entry yet, so create new one
                    userEditsCacheEntry = new()
                    {
                        ReqIdVsState = new(),
                    };
                }
                userEditsCacheEntry.ReqIdVsState[req.ReqId] = (int)MPMUserEditReqState.Intermediate;
                cache.Set(userEditsCacheKey, userEditsCacheEntry);
                Console.WriteLine($"BuildFromData: Cache rows completed for request {req.ReqId}");
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
    }

    
}
