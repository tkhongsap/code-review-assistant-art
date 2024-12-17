using Argento.ReportingService.DL.Users;

namespace Argento.ReportingService.DL.Authentications
{
    public class LoginInfo
    {
        public string LoginToken { get; set; }
        public RequestedUser RequestedUser { get; set; }
    }
}