using System.Net.NetworkInformation;

namespace RAppsAPI.Data
{
    public class Constants
    {
        public enum ResponseReturnCode
        {
            Error = -1,
            InternalError = -2,
            Success = 1
        };

        public const int MAX_SERIES_NUM_IN_RANGE = 10000; // to get all series, there cant be 10000 series in a range
        
    }
}
