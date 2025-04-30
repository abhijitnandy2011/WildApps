using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RAppsAPI.Data;
using RAppsAPI.Models.MPM;
using static RAppsAPI.Data.DBConstants;

namespace RAppsAPI.Services
{
    public class MPMService(RDBContext context,
        IMemoryCache _memoryCache,
        IMPMBuildCacheService _buildCacheFromDBService,
        IServiceProvider _serviceProvider) : IMPMService
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
                int pid = 0, ptid=0, rid = 0;
                MPMProductInfo currProd = null;
                MPMProductTypeInfo currProdType = null;
                MPMRangeInfo currRange = null;
                for (int i = 0; i < rs.Length; i++)
                {
                    var p = rs[i].ProductName;
                    var pt = rs[i].ProductTypeName;
                    var r = rs[i].RangeName;
                    if(currProd == null || currProd.ProductName != p)
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
                string exMsg = ex.Message;
                if (ex.InnerException != null)
                {
                    exMsg += "; InnerException:" + ex.InnerException.Message;
                }
                return new MPMGetProductInfoResponseDTO()
                {
                    Code = (int)Constants.ResponseReturnCode.InternalError,
                    Message = "Failed to read folder:" + exMsg,
                    Products = new()
                };
            }
        }


        // Get Range Info
        /*public async Task<MPMGetRangeInfoResponseDTO> GetRangeInfo(int fileId, int rangeId, int? fromSeries, int? toSeries)
        {
            try
            {
                int fromSeriesNum = fromSeries ?? 1;
                int toSeriesNum = toSeries ?? Constants.MAX_SERIES_NUM_IN_RANGE;  // all series

                //var ri = new MPMRangeInformation();
                // Get series count
                var rs1 = from c in context.Cells
                    let seriesCount = context.Series.Count(s => s.VfileId == fileId && s.RangeId == rangeId && s.Rstatus == (byte)RStatus.Active)
                    let selRange = context.Ranges.FirstOrDefault(r => r.VfileId == fileId && r.RangeId == rangeId && r.Rstatus == (byte)RStatus.Active)
                    where c.VfileId == fileId && c.Rstatus == (byte)RStatus.Active && c.TableId == selRange.HeaderTableId
                    select new MPMRangeInfoResult(
                        seriesCount,
                        c.CellId,
                        c.RowNum,
                        c.ColNum,
                        c.Value,
                        c.Formula,
                        c.Format,
                        c.Style,
                        c.Comment
                    );
                var ri = new MPMRangeInformation();
                if (rs1 != null && rs1.Count() > 0)
                {
                    if(rs1.ElementAt(0).SeriesCount < 0)
                    {
                        throw new Exception("GetRangeInfo: SeriesCount < 0");
                    }
                    ri.NumSeriesActual = rs1.ElementAt(0).SeriesCount;
                    ri.Fields = new();  // this is a List of rows
                    foreach (var res in rs1)  // for each row in resultset
                    {
                        // TODO: Test if rows are being added properly
                        if (res.RowNum < 1)
                        {
                            throw new Exception("GetRangeInfo: res.RowNum < 1");
                        }
                        if (res.ColNum < 1)
                        {
                            throw new Exception("GetRangeInfo: res.ColNum < 1");
                        }
                        var numRowsToAdd = res.RowNum - ri.Fields.Count;
                        for (int i=0; i< numRowsToAdd; ++i)
                        {
                            ri.Fields.Add(new() { Name = "", Cells = new() });
                        }
                        if (res.ColNum == 1)
                        {
                            // Set the field name for API POST calls later
                            ri.Fields[res.RowNum - 1].Name = res.Value;
                        }
                        AddFieldCellsWithColNumCheck(ri, res);                     
                    }
                    ri.RangeId = rangeId;                    
                }

                // Get Series Header info
                var si = new MPMSeriesInformation();
                // TODO - SeriesNum of subsequent series must be updated when a series is added/removed
                var rangeIdParam = new SqlParameter("rangeIdParam", rangeId);
                var vFileIdParam = new SqlParameter("vFileIdParam", fileId);
                var activeStatusParam = new SqlParameter("activeStatusParam", DBConstants.RStatus.Active);
                var fromSeriesNumParam = new SqlParameter("fromSeriesNumParam", fromSeriesNum);
                var toSeriesNumParam = new SqlParameter("toSeriesNumParam", toSeriesNum);
                var listHeaderResults = await context.Database.SqlQuery<MPMSeriesHeaderInfoQueryResult>(
                         @$"SELECT s.SeriesID, s.SeriesNum, c.RowNum, c.ColNum, c.Value, c.Formula, c.Format, c.Style, c.Comment
                            FROM mpm.MSeries AS s
                            JOIN mpm.Cells AS c ON c.VFileID=s.VFileID AND c.TableID=s.HeaderTableID AND c.RStatus={activeStatusParam}
                            WHERE s.RangeID={rangeIdParam} AND s.VFileID={vFileIdParam} AND s.RStatus={activeStatusParam} AND
                                    s.SeriesNum >= {fromSeriesNumParam} AND s.SeriesNum <= {toSeriesNumParam}
                            ORDER BY s.SeriesNum, s.SeriesID")
                        .ToListAsync();

                if (listHeaderResults != null && listHeaderResults.Count > 0)
                {
                    // Get Series Detail info                
                    var listDetailResults = await context.Database.SqlQuery<MPMSeriesHeaderInfoQueryResult>(
                        @$"SELECT s.SeriesID, s.SeriesNum, c.RowNum, c.ColNum, c.Value, c.Formula, c.Format, c.Style, c.Comment
                        FROM mpm.MSeries AS s
                        JOIN mpm.Cells AS c ON c.VFileID=s.VFileID AND c.TableID=s.DetailTableID AND c.RStatus={activeStatusParam}
                        WHERE s.RangeID={rangeIdParam} AND s.VFileID={vFileIdParam} AND s.RStatus={activeStatusParam} AND
                                s.SeriesNum >= {fromSeriesNumParam} AND s.SeriesNum <= {toSeriesNumParam}
                        ORDER BY s.SeriesNum, s.SeriesID")
                        .ToListAsync();
                    // Get series detail tables dims
                    var listDetailDimsResults = await context.Database.SqlQuery<MPMSeriesDetailDimsQueryResult>(
                        @$"SELECT SeriesID, NumRows, NumCols
                        FROM mpm.MTables
                        WHERE VFileID={vFileIdParam} AND RangeID={rangeIdParam} AND TableType>=100 AND RStatus={activeStatusParam}")
                        .ToListAsync();
                    if (listDetailResults != null && listDetailDimsResults != null && 
                        listDetailResults.Count > 0 && listDetailDimsResults.Count > 0)
                    {
                        si.Series = new();                        
                        AddSeriesInfo(si, listHeaderResults, listDetailResults, listDetailDimsResults, fromSeriesNum, toSeriesNum);  
                    }
                }
                // Parse the result set into a hierarchical structure
                // Get the mock APIs out tomorrow
                return new MPMGetRangeInfoResponseDTO()
                {
                    Code = 0,
                    Message = "success",
                    RangeInfo = ri,
                    SeriesInfo = si
                };                
            }
            catch (Exception ex)
            {
                // TODO: Log error
                string exMsg = ex.Message;
                if (ex.InnerException != null)
                {
                    exMsg += "; InnerException:" + ex.InnerException.Message;
                }                
                return new MPMGetRangeInfoResponseDTO()
                {
                    Code = (int)Constants.ResponseReturnCode.InternalError,
                    Message = "Failed to read folder:" + exMsg,
                    RangeInfo = new(),
                    SeriesInfo = new()
                };
            }
        }



        private static void AddSeriesInfo(
            MPMSeriesInformation si,
            List<MPMSeriesHeaderInfoQueryResult> listHeaderResults,
            List<MPMSeriesHeaderInfoQueryResult> listDetailResults,
            List<MPMSeriesDetailDimsQueryResult> listDetailDimsResults,
            int fromSeriesNum, int toSeriesNum)
        {
            // Fill series header info for all series
            foreach (var res in listHeaderResults)
            {
                var numSeriesRows = res.SeriesNum - si.Series.Count - fromSeriesNum + 1;
                for (int i = 0; i < numSeriesRows; i++)
                {
                    si.Series.Add(new());
                }
                // Figure out the series to update from this res
                var index = res.SeriesNum - fromSeriesNum;
                si.Series[index].SeriesId = res.SeriesID;
                si.Series[index].SeriesNum = res.SeriesNum;
                if (si.Series[index].SeriesHeader == null)
                {
                    si.Series[index].SeriesHeader = new();
                    si.Series[index].SeriesHeader.Fields = new();
                }
                // Fill the series header
                var sh = si.Series[index].SeriesHeader;
                var numFieldsToAdd = res.RowNum - sh.Fields.Count;
                for (int i = 0; i < numFieldsToAdd; ++i)
                {
                    sh.Fields.Add(new() { Name = "", Cells = new() });
                }
                if (res.ColNum == 1)
                {
                    // Set the field name for API POST calls later
                    sh.Fields[res.RowNum - 1].Name = res.Value;
                }
                // Fill the field cells/values
                var currRow = sh.Fields[res.RowNum - 1];
                AddSeriesCells(res, currRow.Cells, false);  // no need to drop empty cells for series header
            }
            // Fill series detail info for all series
            var dictSeriesIdVsDims = new Dictionary<int, (int rows, int cols)>();
            foreach (var res in listDetailResults)
            {
                // Figure out the series to update from this res
                var index = res.SeriesNum - fromSeriesNum;
                if (index >= si.Series.Count)
                {
                    throw new Exception($"AddSeriesInfo: {index} >= si.Series.Count while parsing listDetailResults");
                }
                if (si.Series[index].SeriesDetail == null)
                {
                    si.Series[index].SeriesDetail = new();
                    si.Series[index].SeriesDetail.NumRows = -1;  // unknown at this point
                    si.Series[index].SeriesDetail.NumCols = -1;
                    si.Series[index].SeriesDetail.Rows = new();  // equiv of Fields in series header
                }
                var sd = si.Series[index].SeriesDetail;
                // Fill the series detail for this iteration
                var numRowsToAdd = res.RowNum - sd.Rows.Count;
                // Expand Rows to accomodate cells of row RowNum
                var rowNum = sd.Rows.Count;
                for (int i = 0; i < numRowsToAdd; ++i)
                {
                    sd.Rows.Add(new() { RN = ++rowNum, Cells = new() });
                }
                // Fill the series detail cells
                var currRow = sd.Rows[res.RowNum - 1];
                AddSeriesCells(res, currRow.Cells, true);  // drop empty cells for series detail 
                // Update num rows & cols if either is greater
                if (!dictSeriesIdVsDims.ContainsKey(res.SeriesID)) {
                    dictSeriesIdVsDims[res.SeriesID] = (0, 0);
                }
                if (res.RowNum > dictSeriesIdVsDims[res.SeriesID].rows)
                {
                    dictSeriesIdVsDims[res.SeriesID] = (res.RowNum, dictSeriesIdVsDims[res.SeriesID].cols);
                }
                if (res.ColNum > dictSeriesIdVsDims[res.SeriesID].cols)
                {
                    dictSeriesIdVsDims[res.SeriesID] = (dictSeriesIdVsDims[res.SeriesID].rows, res.ColNum);
                }
            }
            // Create the series detail tables dimensions map from DB data
            var dictSeriesIdVsDetailDims = new Dictionary<int, (int numRows, int numCols)>();
            foreach (var table in listDetailDimsResults)
            {
                dictSeriesIdVsDetailDims[table.SeriesID] = (table.NumRows, table.NumCols);
            }
            // Update NumRows & NumCols from dict after checking against DB values
            foreach (var ser in si.Series)
            {
                // Confirm the rows & cols of each series detail table calculated in this function
                // matches whats in the DB
                var currSeries = dictSeriesIdVsDims[ser.SeriesId];
                if (currSeries.rows != dictSeriesIdVsDetailDims[ser.SeriesId].numRows)
                {
                    throw new Exception($"AddSeriesInfo: NumRows calc in function({currSeries.rows}) does not match DB({dictSeriesIdVsDetailDims[ser.SeriesId].numRows})");
                }
                if (currSeries.cols != dictSeriesIdVsDetailDims[ser.SeriesId].numCols)
                {
                    throw new Exception($"AddSeriesInfo: NumCols calc in function({currSeries.cols}) does not match DB({dictSeriesIdVsDetailDims[ser.SeriesId].numCols})");
                }
                // Checks complete & successful
                ser.SeriesDetail.NumRows = currSeries.rows;
                ser.SeriesDetail.NumCols = currSeries.cols;                
            }

        }


        // Add series cells for series header or detail, as the List of Cells passed may belong to
        private static void AddSeriesCells(MPMSeriesHeaderInfoQueryResult res, List<MPMRichCell> Cells, bool dropEmptyCells)
        {
            if (dropEmptyCells &&
                res.Value.Length == 0 &&
                res.Formula.Length == 0 &&
                res.Format.Length == 0 &&
                res.Style.Length == 0 &&
                res.Comment.Length == 0)
            {
                // Ignore this cell
                return;
            }           
            Cells.Add(new() { 
                CN = res.ColNum,            
                Value = res.Value,
                VType = "",   // this is currently not loaded in db, format will have the type
                Formula = res.Formula,
                Format = res.Format,
                Style = res.Style,
                Comment = res.Comment
            });
        }



        // Add cells in row, assumes the proper row is available at the proper position.
        // Check if Cells has enough columns or add to it
        // TODO: Test if columns are being added properly with correct CN value
        private static void AddFieldCellsWithColNumCheck(MPMRangeInformation ri, MPMRangeInfoResult res)
        {
            var currRow = ri.Fields[res.RowNum-1];
            var numColsToAdd = res.ColNum - currRow.Cells.Count;
            var colNum = currRow.Cells.Count;
            for (int i = 0; i < numColsToAdd; ++i)
            {
                currRow.Cells.Add(new() { CN = ++colNum });
            }
            var index = res.ColNum - 1;           
            currRow.Cells[index].Value = res.Value;
            currRow.Cells[index].VType = "";   // this is currently not loaded in db, format will have the type
            currRow.Cells[index].Formula = res.Formula;
            currRow.Cells[index].Format = res.Format;
            currRow.Cells[index].Style = res.Style;
            currRow.Cells[index].Comment = res.Comment;
        }
        */

        // Get series detail data
        public async Task<MPMReadRequestResponseDTO> GetFileRows(MPMReadRequestDTO readDTO)
        {
            // Check if cache has rows
            MPMReadRequestResponseDTO response;
            var retCode = GatherRowsFromCache(readDTO, out response);
            if (retCode == 0)
            {
                return response;
            }
            // Try to build the cache from DB
            int triesLeft = 3;
            bool cacheBuilt = false;
            do
            {
                retCode = await _buildCacheFromDBService.BuildFromDB(readDTO, 0, _serviceProvider);
                if (retCode == 0)
                {
                    cacheBuilt = true;
                    break;
                }                
                --triesLeft;
                Console.WriteLine($"GetFileRows: Failed to build cache: req {readDTO.ReqId}, tries left:{triesLeft}");
            } while (triesLeft > 0);
            if (cacheBuilt)
            {
                // Try to gather rows again
                retCode = GatherRowsFromCache(readDTO, out response);
                if (retCode == 0)
                {
                    return response;
                }
                else
                {
                    response.Code = -2;
                    Console.WriteLine($"GetFileRows: Failed to get rows even after building cache: req {readDTO.ReqId}");
                }
            }
            else
            {
                // Failed to build cache after multiple tries                
                response.Code = -1;
                Console.WriteLine($"GetFileRows: Failed to build cache after multiple tries: req {readDTO.ReqId}");
            }
            response.Message = "Failed to get rows.";            
            return response;
        }

        // Try to gather rows from cache, or return failed
        private int GatherRowsFromCache(MPMReadRequestDTO req, out MPMReadRequestResponseDTO response)
        {
            response = new()
            {
                Code = 0,
                Message = "",
                ReqId = req.ReqId,
                FileId = req.FileId,
                Sheets = new()
            };
            int retCode = 0;  // 1 - warning, not all sheets found, 2 - not all rows found in some sheets, 3-both
            foreach (var sheet in req.Sheets)
            {
                var sheetName = sheet.SheetName;
                var cacheKey = "MPM_" + req.FileId + "_" + sheetName;
                MPMSheetCacheEntry? entry;
                var success = _memoryCache.TryGetValue(cacheKey, out entry);
                if (!success || entry == null)
                {
                    Console.WriteLine($"GatherRowsFromCache: Cache key not found, for sheet, req:{req.ReqId}, key:{cacheKey}, sheet:{sheetName}");
                    retCode |= 0x1;
                    break;  // TODO: If we want to gather sheets which are there anyway, then continue
                }
                // Sheet cache entry is there
                MPMReadResponseSheet sheetEntry = new()
                {
                    SheetName = sheetName,
                    Rows = new(),
                };                
                // Check and add rows
                var dict = entry.RowNumberVsRowEntry;
                HashSet<int> setEmptyRows = new HashSet<int>(entry.EmptyRows);
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
                        Console.WriteLine($"GatherRowsFromCache: Row not found in cache dict nor empty rows set, req:{req.ReqId}, key:{cacheKey}, row:{r}");
                        retCode |= 0x2;
                        break; // TODO: If we want to gather rows in this sheet which are there anyway, then continue
                    }
                    // Gather available row
                    var cacheRowEntry = dict[r];
                    var responseRow = new MPMReadResponseRow()
                    {
                        RN = cacheRowEntry.Row.RN,
                        State = (cacheRowEntry.State == MPMCacheRowState.DB) ? 1 : 2,
                        Cells = cacheRowEntry.Row.Cells, // TODO: Can cache entry be lost while req is processed/before response?
                                                         // GC should not clear this even if cache entry nulls
                    };
                    sheetEntry.Rows.Add(responseRow);
                }
                // Tables
                // TODO: Check and add the tables from this sheet
                // Add to response only at end after all rows gathered for this sheet
                response.Sheets.Add(sheetEntry);
            } // for-sheet
            return retCode;
        }


    }

    
}
