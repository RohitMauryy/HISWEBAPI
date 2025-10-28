namespace HISWEBAPI.DTO.User
{
    public class LoginSessionRequest
    {
        public long UserId { get; set; }
        public int BranchId { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string Browser { get; set; }
        public string BrowserVersion { get; set; }
        public string OperatingSystem { get; set; }
        public string Device { get; set; }
        public string DeviceType { get; set; }
        public string Location { get; set; }
    }

    public class LoginHistoryResponse
    {
        public long SessionId { get; set; }
        public DateTime LoginTime { get; set; }
        public DateTime? LogoutTime { get; set; }
        public string IpAddress { get; set; }
        public string Browser { get; set; }
        public string OperatingSystem { get; set; }
        public string Device { get; set; }
        public string Location { get; set; }
        public string Status { get; set; }
        public string LogoutReason { get; set; }
    }

    public class ActiveSessionResponse
    {
        public long SessionId { get; set; }
        public DateTime LoginTime { get; set; }
        public DateTime LastActivityTime { get; set; }
        public string IpAddress { get; set; }
        public string Browser { get; set; }
        public string OperatingSystem { get; set; }
        public string Device { get; set; }
        public bool IsCurrentSession { get; set; }
    }
}