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

        public const int MAX_COLS_READ_IN_SHEET = 500;

        public const int CACHE_BUILD_FROM_DB_NUM_TRIES = 3;


        //----------------------------------------
        // Cache Keys
        //----------------------------------------
        public static string GetCompletedEditRequestsCacheKey(int userId)
        {
            return $"MPM_EditReqList_{userId}";
        }

        public static string GetFailedEditRequestsCacheKey(int userId)
        {
            return $"MPM_FailedEditReqList_{userId}";
        }        

        public static string GetWorkbookSheetCacheKey(int fileId, string sheetName)
        {
            return $"MPM_{fileId}_{sheetName}";
        }

    }
}
