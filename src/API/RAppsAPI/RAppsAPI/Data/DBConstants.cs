using System.Net.NetworkInformation;

namespace RAppsAPI.Data
{
    // Some DB values which do not often change are hardcoded here to avoid DB lookups
    // for simple things. Change this policy as needed.
    public class DBConstants
    {
        public enum RStatus
        {
            Inactive = 1,
            Active = 2
        };

        public static class RoleName
        {
            public static readonly string Unassigned = "Unassigned";
            public static readonly string Visitor = "Visitor";
            public static readonly string Admin = "Admin";
        }

        // System IDs
        public static int USER_SYSTEM_ID = 2;

        // User IDs
        public static int SYSTEM_USER_ID = 1;
        public static int ADMIN_USER_ID = 2;
        public static int MIN_NON_ADMIN_ID = 101;

        public enum FolderObjectType
        {
            Folder = 1,
            File = 2,
            Link = 3
        };

        public static string PathSep = "/";
        public static string PathIDSep = ",";
    }
}
