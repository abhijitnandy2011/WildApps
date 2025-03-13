using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.SqlServer.Server;
using RAppsAPI.Data;
using RAppsAPI.Models.MPM;
using System.ComponentModel;
using static RAppsAPI.Data.DBConstants;

namespace RAppsAPI.Services
{
    public class MPMService(RDBContext context) : IMPMService
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
        public async Task<MPMGetRangeInfoResponseDTO> GetRangeInfo(int fileId, int rangeId, int? fromSeries, int? toSeries)
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

                // Get Series Detail info                
                var listDetailResults = await context.Database.SqlQuery<MPMSeriesHeaderInfoQueryResult>(
                         @$"SELECT s.SeriesID, s.SeriesNum, c.RowNum, c.ColNum, c.Value, c.Formula, c.Format, c.Style, c.Comment
                            FROM mpm.MSeries AS s
                            JOIN mpm.Cells AS c ON c.VFileID=s.VFileID AND c.TableID=s.DetailTableID AND c.RStatus={activeStatusParam}
                            WHERE s.RangeID={rangeIdParam} AND s.VFileID={vFileIdParam} AND s.RStatus={activeStatusParam} AND
                                    s.SeriesNum >= {fromSeriesNumParam} AND s.SeriesNum <= {toSeriesNumParam}
                            ORDER BY s.SeriesNum, s.SeriesID")
                        .ToListAsync();

                var si = new MPMSeriesInformation
                {
                    Series = new List<MPMSeriesInfoRow>
                    {
                        new()
                        {
                           SeriesId = 1,
                           SeriesNum = 1,
                           SeriesHeader = new()
                           {
                              Fields = new()
                              {
                                new(){ Name = "Table Name",  Cells = new() { new() {CN=1, Value = "RangeSeriesTable;XUV700" } } },
                                new(){ Name = "To Be Processed",  Cells = new() { new() {CN=1, Value = "Yes" } } }
                              }
                           },
                           SeriesDetail = new()
                           {
                               NumRows = 3,   // front end should check the dimensions before accepting
                               NumCols = 3,
                               Rows = new()
                               {
                                   new(){  // Row 1
                                       RN = 1,     
                                       Cells = new()
                                       {
                                           new(){CN=1, Value="Table Name" },
                                           new(){CN=2, Value="RangeSeriesDetailTable;XUV700" },
                                           new(){CN=3, Value="" }
                                       }
                                   },
                                   new(){  // Row 2
                                       RN = 2,
                                       Cells = new()
                                       {
                                           new(){CN=1, Value="To Be Processed"},
                                           new(){CN=2, Value="Yes"},
                                           new(){CN=3, Value=""}
                                       }
                                   },
                                   new(){  // Row 3
                                       RN = 3,
                                       Cells = new()
                                       {
                                           new(){CN=1, Value="XUV700"},
                                           new(){CN=2, Value="Type"},
                                           new(){CN=3, Value="MX"}
                                       }
                                   }
                               }
                           }
                        }                        
                    }
                };

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
    }

    
}
