using System.ComponentModel.DataAnnotations.Schema;

namespace RAppsAPI.Models.MPM
{
    public class MPMAddWBEditResult
    {
        public int EditID { get; set; }
        public int? LockedBy { get; set; }
        public DateTime LastLockedTime { get; set; }
        public int RetCode { get; set; }
        public string Message { get; set; }
    }

    public class MPMUpdateWBEditResult : MPMAddWBEditResult;

}
