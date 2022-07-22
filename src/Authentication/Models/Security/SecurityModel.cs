namespace Authentication.Models.Security
{
    public class SecurityModel
    {
        public List<SessionInfoModel> Sessions { get; set; } = new List<SessionInfoModel>();

        public List<LoginInfoModel> Logins { get; set; } = new List<LoginInfoModel>();
    }

    public class SessionInfoModel
    {
        public DateTime LastSeen { get; set; }

        public string BrowserFamily { get; set; }

        public string BrowserVersion { get; set; }

        public string DeviceFamily { get; set; }

        public string OS { get; set; }
    }

    public class LoginInfoModel
    {
        public DateTime LoggedOnAt { get; set; }

        public string BrowserFamily { get; set; }

        public string BrowserVersion { get; set; }

        public string DeviceFamily { get; set; }

        public string OS { get; set; }
    }
}
