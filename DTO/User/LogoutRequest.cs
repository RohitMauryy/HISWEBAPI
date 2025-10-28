namespace HISWEBAPI.DTO.User
{
    public class LogoutRequest
    {
        public long SessionId { get; set; }
        public string LogoutReason { get; set; }
    }
}
