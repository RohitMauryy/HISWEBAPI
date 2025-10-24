namespace HISWEBAPI.DTO
{
    public class LoginRequest
    {
        public int BranchId { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
