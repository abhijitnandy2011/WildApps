// TODO: Unit testing needed of editing, adding, removing rows, sheets & tables
// Directly applying row data in between without accounting for rows added/removed
// will lead to data corruption. Same with cells/columns

using System.Text.Json.Serialization;
using RAppsAPI.Models.MPM;

namespace RAppsAPI.Models.MPM
{
    public class MPMFailedEditReqInfoInternal
    {
        public MPMEditRequestDTO Req { get; set; } // unique Id set by client to track req
        public int UserId { get; set; }
        public int Code { get; set; } = -1;
        public string Message { get; set; } = string.Empty;

    }

    
}




