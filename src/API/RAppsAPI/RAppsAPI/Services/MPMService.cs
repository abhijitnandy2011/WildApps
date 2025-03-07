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
                    new { ProductName = "Big Car", ProductTypeName="SUV", RangeName="XUV700"},
                    new { ProductName = "Big Car", ProductTypeName="SUV", RangeName="XUV700"},
                    new { ProductName = "Big Car", ProductTypeName="SUV", RangeName="XUV700"},
                    new { ProductName = "Big Car", ProductTypeName="SUV", RangeName="XUV700"},
                    new { ProductName = "Big Car", ProductTypeName="SUV", RangeName="XUV700"},
                    new { ProductName = "Big Car", ProductTypeName="SUV", RangeName="XUV700"},
                    new { ProductName = "Big Car", ProductTypeName="SUV", RangeName="XUV700"},
                    new { ProductName = "Big Car", ProductTypeName="SUV", RangeName="XUV700"},
                    new { ProductName = "Big Car", ProductTypeName="SUV", RangeName="XUV700"},
                    new { ProductName = "Big Car", ProductTypeName="SUV", RangeName="XUV700"},
                    new { ProductName = "Big Car", ProductTypeName="SUV", RangeName="XUV700"},
                    new { ProductName = "Big Car", ProductTypeName="SUV", RangeName="XUV700"},
                    new { ProductName = "Big Car", ProductTypeName="SUV", RangeName="XUV700"},
                    new { ProductName = "Big Car", ProductTypeName="SUV", RangeName="XUV700"},
                    new { ProductName = "Big Car", ProductTypeName="SUV", RangeName="XUV700"},
                    new { ProductName = "Big Car", ProductTypeName="SUV", RangeName="XUV700"},
                };

                // Parse the result set into a hierarchical structure
                // Get the mock APIs out tomorrow


                var products = new List<MPMProductInfo>
                {
                    // Big Car
                    new MPMProductInfo
                    {
                        ProductId = 1,
                        ProductName = "Big Car",
                        ProductTypeInfo = new List<MPMProductTypeInfo>
                        {
                            new MPMProductTypeInfo
                            {
                                ProductTypeId = 1,
                                ProductTypeName = "SUV",
                                RangeInfo = new List<MPMRangeInfo>
                                {
                                    new MPMRangeInfo
                                    {
                                        RangeId = 1,
                                        RangeName = "Mahindra",
                                        imageUrl = ""
                                    }
                                }
                            }
                        }
                    },

                    new MPMProductInfo
                    {
                        ProductId = 1,
                        ProductName = "Big Car",
                        ProductTypeInfo = new List<MPMProductTypeInfo>
                        {
                            new MPMProductTypeInfo
                            {
                                ProductTypeId = 1,
                                ProductTypeName = "SUV",
                                RangeInfo = new List<MPMRangeInfo>
                                {
                                    new MPMRangeInfo
                                    {
                                        RangeId = 2,
                                        RangeName = "Thar",
                                        imageUrl = ""
                                    }
                                }
                            }
                        }
                    },
                    
                    // Little Car
                    new MPMProductInfo
                    {
                        ProductId = 2,
                        ProductName = "Little Car",
                        ProductTypeInfo = new List<MPMProductTypeInfo>
                        {
                            new MPMProductTypeInfo
                            {
                                ProductTypeId = 2,
                                ProductTypeName = "Hatchback",
                                RangeInfo = new List<MPMRangeInfo>
                                {
                                    new MPMRangeInfo
                                    {
                                        RangeId = 3,
                                        RangeName = "Maruti",
                                        imageUrl = ""
                                    }
                                }
                            }
                        }
                    },

                    new MPMProductInfo
                    {
                        ProductId = 2,
                        ProductName = "Little Car",
                        ProductTypeInfo = new List<MPMProductTypeInfo>
                        {
                            new MPMProductTypeInfo
                            {
                                ProductTypeId = 2,
                                ProductTypeName = "Hatchback",
                                RangeInfo = new List<MPMRangeInfo>
                                {
                                    new MPMRangeInfo
                                    {
                                        RangeId = 4,
                                        RangeName = "Ambassador",
                                        imageUrl = ""
                                    }
                                }
                            }
                        }
                    }
                };

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
