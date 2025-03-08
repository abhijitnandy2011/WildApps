using RAppsAPI.Data;
using RAppsAPI.Models.MPM;
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
                var ri = new MPMRangeInformation
                {
                    RangeId = 1,
                    NumSeriesActual = 4,
                    Fields = new()
                    {
                        new(){ Name = "Table Name",  Values = new() { "RangeHeaderTable;XUV700,XUV500" } },
                        new(){ Name = "To Be Processed",  Values = new() { "Yes" } }
                    }
                };

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
                                new(){ Name = "Table Name",  Values = new() { "RangeSeriesTable;XUV700" } },
                                new(){ Name = "To Be Processed",  Values = new() { "Yes" } }
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

    }
}
