﻿using System.Net.NetworkInformation;

namespace RAppsAPI.Data
{
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


    }
}
