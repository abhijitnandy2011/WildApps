namespace RAppsAPI.Models.MPM
{
    public class MPMWBMetaInfo
    {
        public int WriteFrequencyInSeconds { get; set; }
        public DateTime LastWriteTime { get; set; }
        public bool IsModified { get; set; }
    }    

}
