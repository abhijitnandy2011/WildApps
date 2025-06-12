using Azure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RAppsAPI.Data;
using RAppsAPI.ExcelUtils;
using RAppsAPI.Models.MPM;
using Serilog;
using static RAppsAPI.Data.DBConstants;

namespace RAppsAPI.Services
{
    public class MPMService(RDBContext context,
        IMemoryCache _memoryCache,
        IMPMBuildCacheService _buildCacheFromDBService,
        IServiceProvider _serviceProvider,
        IMPMBackgroundRequestQueue _reqQueue) : IMPMService
    {
        public async Task<MPMGetProductInfoResponseDTO> GetProductInfo(int fileId)
        {
            try
            {
                /*var dbObjList = await context.Products
                    .Include(p => p.ProductTypes)
                    .Include(p => p.ProductTypes)
                    .ThenInclude(pt => pt.MRanges)                    
                    .Where(p => p.VfileId == fileId && p.Rstatus == (byte)RStatus.Active &&
                            p.ProductTypes.All(pt => pt.VfileId == fileId && pt.Rstatus == (byte)RStatus.Active &&
                              pt.MRanges.All(r => r.VfileId == fileId && r.Rstatus == (byte)RStatus.Active)))                        
                    .ToListAsync();*/

                var query = from p in context.Products
                            join pt in context.ProductTypes on p.ProductId equals pt.ProductId
                            join r in context.Ranges on pt.ProductTypeId equals r.ProductTypeId
                            where p.VfileId == fileId && p.Rstatus == (byte)RStatus.Active &&
                                    pt.VfileId == fileId && pt.Rstatus == (byte)RStatus.Active &&
                                      r.VfileId == fileId && r.Rstatus == (byte)RStatus.Active
                            select new
                            {
                                ProductName = p.Name,
                                ProductTypeName = pt.Name,
                                RangeName = r.Name
                            };

                var rs = new[]
                {
                    new { ProductName = "Big Car", ProductTypeName="SUV", RangeName="SUV700"},
                    new { ProductName = "Big Car", ProductTypeName="SUV", RangeName="SUV800"},
                    new { ProductName = "Big Car", ProductTypeName="SUV", RangeName="SUV900"},
                    new { ProductName = "Big Car", ProductTypeName="SUV", RangeName="SUV1000"},
                    new { ProductName = "Big Car", ProductTypeName="XUV", RangeName="XUV500"},
                    new { ProductName = "Big Car", ProductTypeName="XUV", RangeName="XUVA00"},
                    new { ProductName = "Big Car", ProductTypeName="XUV", RangeName="XUVB00"},
                    new { ProductName = "Little Car", ProductTypeName="XUV", RangeName="XUV700"},
                    new { ProductName = "Little Car", ProductTypeName="XUV", RangeName="XUV800"},
                    new { ProductName = "Little Car", ProductTypeName="XUV", RangeName="XUV900"},
                    new { ProductName = "Little Car", ProductTypeName="XUV", RangeName="XUV1000"},
                    new { ProductName = "Little Car", ProductTypeName="SUV", RangeName="XUVX00"},
                    new { ProductName = "Little Car", ProductTypeName="SUV", RangeName="XUVY00"},
                    new { ProductName = "Little Car", ProductTypeName="SUV", RangeName="XUVZ00"},
                    new { ProductName = "Little Car", ProductTypeName="SUV", RangeName="XUVU00"},
                };

                // Parse the result set into a hierarchical structure
                var products = new List<MPMProductInfo>();
                int pid = 0, ptid = 0, rid = 0;
                MPMProductInfo currProd = null;
                MPMProductTypeInfo currProdType = null;
                MPMRangeInfo currRange = null;
                for (int i = 0; i < rs.Length; i++)
                {
                    var p = rs[i].ProductName;
                    var pt = rs[i].ProductTypeName;
                    var r = rs[i].RangeName;
                    if (currProd == null || currProd.ProductName != p)
                    {
                        currProd = new MPMProductInfo();
                        products.Add(currProd);
                        currProd.ProductId = ++pid;
                        currProd.ProductName = p;
                        currProd.ProductTypeInfo = new List<MPMProductTypeInfo>();
                        currProdType = null;
                    }
                    if (currProdType == null || currProdType.ProductTypeName != pt)
                    {
                        currProdType = new MPMProductTypeInfo();
                        currProd.ProductTypeInfo.Add(currProdType);
                        currProdType.ProductTypeId = ++ptid;
                        currProdType.ProductTypeName = pt;
                        currProdType.RangeInfo = new List<MPMRangeInfo>();
                        currRange = null;
                    }
                    if (currRange == null || currRange.RangeName != r)
                    {
                        currRange = new MPMRangeInfo();
                        currProdType.RangeInfo.Add(currRange);
                        currRange.RangeId = ++rid;
                        currRange.RangeName = r;
                        currRange.imageUrl = "";
                    }
                }

                // Send Product response
                return new MPMGetProductInfoResponseDTO
                {
                    Code = 0,
                    Message = "success",
                    Products = products,
                };

            }
            catch (Exception ex)
            {
                // TODO: Log error
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Ex: {ex.InnerException.Message}\n Trace:{ex.StackTrace}");
                }
                return new MPMGetProductInfoResponseDTO()
                {
                    Code = (int)Constants.ResponseReturnCode.InternalError,
                    Message = "Failed to read folder:" + ex.Message,
                    Products = new()
                };
            }
        }


        // Get file rows
        // Return codes:
        //    -1 Exception
        //    -2 failed to build cache after multiple tries
        //    -3 failed to get rows even after building cache
        //     1 not all sheets found
        //     2 not all rows found in some sheets
        //     3 both the above
        //    10 Not all edit requests that were asked to be checked, have completed, chk field IncompleteEditRequests
        // 
        public async Task<MPMReadRequestResponseDTO> GetFileRows(MPMReadRequestDTO readDTO, int userId)
        {
            MPMReadRequestResponseDTO response = new()
            {
                Code = 0,
                Message = "",
                CompletedEditRequests = new(),
                ChkedIncompleteEditRequests = new(),
                FailedEditRequests = new(),
                ReqId = readDTO.ReqId,
                FileId = readDTO.FileId,
                Sheets = new()
            };
            try
            {               
                // First add failed edit requests to response
                var userFailedEditsCacheKey = Constants.GetFailedEditRequestsCacheKey(userId);
                MPMUserFailedEditReqsCacheEntry? userFailedEditsCacheEntry;
                var success = _memoryCache.TryGetValue(userFailedEditsCacheKey, out userFailedEditsCacheEntry);
                if (!success || userFailedEditsCacheEntry == null)
                {
                    Log.Information($"GetFileRows:({userId},{readDTO.ReqId}):Cache key not found, key:{userFailedEditsCacheKey}");
                    // Not an error as the entry will be absent if no failed edits yet
                }
                else
                {
                    // Got the failed edit reqs entry from cache
                    response.FailedEditRequests = userFailedEditsCacheEntry;
                }
                // Second, add completed requests to response
                var userEditsCacheKey = Constants.GetCompletedEditRequestsCacheKey(userId);
                MPMUserEditReqsCacheEntry? userEditsCacheEntry;
                success = _memoryCache.TryGetValue(userEditsCacheKey, out userEditsCacheEntry);
                if (!success || userEditsCacheEntry == null)
                {
                    Log.Information($"GetFileRows:({userId},{readDTO.ReqId}):Cache key not found, key:{userEditsCacheKey}");
                    // Not an error as the entry will be absent if no edits have happened for the sheet
                }
                else
                {
                    // Got the completed edit reqs entry from cache
                    response.CompletedEditRequests = userEditsCacheEntry!.ReqIdVsState;  // TODO: check, return full info as dictionary          
                }
                // Third, check completed edit requests if such a list was given, before doing a read
                int retCode = 0;
                if (((readDTO.CheckCompletedEditReqIds?.Count) ?? 0) > 0)
                {
                    retCode = CheckCompletedEditRequests(readDTO, userId, userEditsCacheEntry.ReqIdVsState, response);
                    if (retCode != 0)
                    {
                        // There was an error or there are incomplete requests, cant proceed with read
                        return response;
                    }
                }
                // Now, can do read, check if cache has rows first
                retCode = GatherRowsFromCache(readDTO, userId, response);
                if (retCode == 0)
                {
                    // Cache has rows, return the rows
                    return response;
                }
                // Cache didnt have rows, try to build the cache from DB
                int triesLeft = Constants.CACHE_BUILD_FROM_DB_NUM_TRIES;
                bool cacheBuilt = false;
                do
                {
                    (retCode, _) = await _buildCacheFromDBService.BuildFromDB(readDTO, userId, 0, _serviceProvider);
                    if (retCode == 0)
                    {
                        cacheBuilt = true;
                        break;
                    }
                    --triesLeft;
                    Console.WriteLine($"GetFileRows:({userId},{readDTO.ReqId}):Failed to build cache, tries left:{triesLeft}");
                } while (triesLeft > 0);
                if (cacheBuilt)
                {
                    // Try to gather rows again
                    retCode = GatherRowsFromCache(readDTO, userId, response);
                    if (retCode == 0)
                    {
                        return response;
                    }
                    else
                    {
                        response.Code = -3;
                        Console.WriteLine($"GetFileRows:({userId},{readDTO.ReqId}):Failed to get rows even after building cache");
                    }
                }
                else
                {
                    // Failed to build cache after multiple tries                
                    response.Code = -2;
                    Console.WriteLine($"GetFileRows:({userId},{readDTO.ReqId}):Failed to build cache after multiple tries");
                }
                response.Message = "Failed to get rows.";
            }
            catch (Exception ex)
            {
                response.Code = -1;
                response.Message = ex.Message;
                // TODO log error
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Ex: {ex.InnerException.Message}\n Trace:{ex.StackTrace}");
                }
            }
            return response;
        }

        // Check if the edit reqs specified by the user have been completed.
        // Returns:
        //    0 if all the requests are complete
        //   10 if not
        //   -1 on error
        private int CheckCompletedEditRequests(
            MPMReadRequestDTO readDTO,
            int userId,
            Dictionary<int, int> dictReqIdVsState,
            MPMReadRequestResponseDTO response)
        {
            int retCode = -1;
            try
            {
                var reqsToBeChked = readDTO.CheckCompletedEditReqIds;
                foreach (var rtc in reqsToBeChked)
                {
                    if (!dictReqIdVsState.ContainsKey(rtc))
                    {
                        // Add incomplete edit req
                        response.ChkedIncompleteEditRequests.Add(rtc);
                    }
                }
                if (response.ChkedIncompleteEditRequests.Count > 0)
                {
                    response.Code = 10;
                    response.Message = $"There are {response.ChkedIncompleteEditRequests.Count} incomplete edit requests";
                }
            }
            catch (Exception ex)
            {
                retCode = -1;
                // TODO log error
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Ex: {ex.InnerException.Message}\n Trace:{ex.StackTrace}");
                }
            }
            return retCode;
        }

        // Try to gather rows from cache, or return failed.
        // Must read at least 1 row to get the table info for a sheet.
        // Returns: 0 success
        //     -1 Exception
        //      1 not all sheets found
        //      2 not all rows found in some sheets
        //      3 both the above
        private int GatherRowsFromCache(
            MPMReadRequestDTO req,
            int userId,
            MPMReadRequestResponseDTO response)
        {
            int retCode = 0;
            try
            {
                foreach (var sheet in req.Sheets)
                {
                    var sheetName = sheet.SheetName;
                    if (sheet.Rects.Count == 0)
                    {
                        Console.WriteLine($"GatherRowsFromCache:({userId},{req.ReqId}):There are no Read Rects given for sheet:{sheetName}. Skipping reads.");
                        continue;
                    }
                    var sheetCacheKey = Constants.GetWorkbookSheetCacheKey(req.FileId, sheetName);
                    MPMSheetCacheEntry? sheetCacheEntry;
                    var success = _memoryCache.TryGetValue(sheetCacheKey, out sheetCacheEntry);
                    if (!success || sheetCacheEntry == null)
                    {
                        Console.WriteLine($"GatherRowsFromCache:({userId},{req.ReqId}):Cache key not found, for sheet, key:{sheetCacheKey}, sheet:{sheetName}");
                        retCode |= 0x1;
                        break;  // TODO: If we want to gather sheets which are there anyway, then continue
                    }
                    // Sheet cache entry is there
                    MPMReadResponseSheet responseSheet = new()
                    {
                        SheetName = sheetName,
                        Rows = new(),
                        Tables = new(),
                    };
                    // Check and add rows
                    var dict = sheetCacheEntry.RowNumberVsRowEntry;
                    HashSet<int> setEmptyRows = new HashSet<int>(sheetCacheEntry.EmptyRows);
                    var rect = sheet.Rects[0];
                    for (var r = rect.top; r <= rect.bottom; r++)
                    {
                        if (setEmptyRows.Contains(r))
                        {
                            continue; // no need to gather empty row
                        }
                        // Row may not be there in dict if e.g. this req is asking for different rows than was cached
                        if (!dict.ContainsKey(r))
                        {
                            Console.WriteLine($"GatherRowsFromCache:({userId},{req.ReqId}):Row not found in cache dict nor empty rows set, key:{sheetCacheKey}, row:{r}");
                            retCode |= 0x2;
                            continue; // We want to gather rows in this sheet which are there anyway, then continue
                        }
                        // Gather available row in cache
                        var sheetCacheRowEntry = dict[r];
                        var responseRow = new MPMReadResponseRow()
                        {
                            RN = sheetCacheRowEntry.Row.RN,
                            State = (sheetCacheRowEntry.State == MPMCacheRowState.Temp) ? 1 : 2,
                            Cells = sheetCacheRowEntry.Row.Cells, // TODO: Can cache entry be lost while req is processed/before response?
                                                                  // GC should not clear this even if cache entry nulls
                        };
                        responseSheet.Rows.Add(responseRow);
                    }
                    // Tables
                    foreach (var table in sheetCacheEntry.Tables)
                    {
                        responseSheet.Tables.Add(new()
                        {
                            TableName = table.TableName,
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
                    // Add to response only at end after all rows gathered for this sheet
                    response.Sheets.Add(responseSheet);
                } // for-sheet    
            }
            catch (Exception ex)
            {
                retCode = -1;
                // TODO log error
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Ex: {ex.InnerException.Message}\n Trace:{ex.StackTrace}");
                }
            }
            return retCode;
        }


        
        // Registers edit for processing, if written to DB successfully
        // Returns: 0 on success
        //         -1 Exception
        //         -2 Failed to write edit to DB first before inserting in queue
        public async Task<MPMEditRequestResponseDTO> EditFile(MPMEditRequestDTO editDTO, int userId)
        {
            // Try to write the edit req to DB first.
            // This can fail due to various locks on the workbook.
            // If failed then req will not be queued.
            int retCode = 0;
            string retMsg = "";
            try
            {
                // TODO: Write edit request to DB first, state as 'Processing' with time.
                // Set 'AppliedUpon' column to current file version.
                WBTools wbTools = new();
                var req = editDTO;
                var resTuple = wbTools.WriteEditToDB(req, userId, context);
                if (resTuple.retCode == 0)
                {
                    MPMBGQCommand qCmd = new()
                    {
                        Command = BGQueueCmd.Edit,
                        UserId = 0,
                        RegdEditId = resTuple.editId,
                        EditReq = editDTO,
                    };
                    _reqQueue.QueueBackgroundRequest(qCmd);
                }
                else
                {
                    // Failed to register edit, so return failure
                    // TODO: Write log
                    var msg = $"ApplyEditsToWorkbook:Failed to register edit, reqId:{req.ReqId}, userId:{userId}";
                    Console.WriteLine(msg);
                    retCode = -2;
                    retMsg = msg;                   
                }               
            }
            catch (Exception ex)
            {
                retCode = -1;
                retMsg = ex.Message;
                // TODO log error
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Ex: {ex.InnerException.Message}\n Trace:{ex.StackTrace}");
                }
            }
            return new()
            {
                Code = retCode,
                Message = retMsg,
            };
        }


    }  // class

    
}  // namespace
