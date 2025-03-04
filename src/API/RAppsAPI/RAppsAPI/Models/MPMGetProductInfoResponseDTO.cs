namespace RAppsAPI.Models.MPM
{
    public class MPMGetProductInfoResponseDTO
    {
        public int Code { get; set; } = -1;
        public string Message { get; set; } = string.Empty;
        public List<MPMProductInfo> Products { get; set; }
    }


    public class MPMProductInfo
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public List<MPMProductTypeInfo> ProductTypeInfo { get; set; }
    }

    public class MPMProductTypeInfo
    {
        public int ProductTypeId { get; set; }
        public string ProductTypeName { get; set; } = string.Empty;
        public List<MPMRangeInfo> RangeInfo { get; set; }
    }
    public class MPMRangeInfo
    {
        public int RangeId { get; set; }
        public string RangeName { get; set; } = string.Empty;
        public string imageUrl { get; set; } = string.Empty;
    }
}
