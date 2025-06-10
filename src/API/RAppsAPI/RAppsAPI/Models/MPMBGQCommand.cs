namespace RAppsAPI.Models.MPM
{
    public class MPMBGQCommand
    {
        public int UserId { get; set; }
        public int RegdEditId { get; set; }  // the edit id from DB
        public MPMEditRequestDTO EditReq { get; set; }
    }



}
