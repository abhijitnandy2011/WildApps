namespace RAppsAPI.Models.MPM
{
    public enum BGQueueCmd
    {
        Edit,
        WriteFiles,
    };

    public class MPMBGQCommand
    {
        public BGQueueCmd Command { get; set; }
        public int UserId { get; set; }
        public int RegdEditId { get; set; }  // the edit id from DB
        public MPMEditRequestDTO EditReq { get; set; }
    }



}
