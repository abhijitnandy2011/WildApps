using RAppsAPI.Models.MPM;

namespace RAppsAPI.Models
{
    public class LoginResponse
    {
        public int Code { get; set; } = -1;
        public string Message { get; set; } = string.Empty;
        public string IdToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
