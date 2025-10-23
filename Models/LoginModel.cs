using System;

namespace HISWEBAPI.Models
{
    public class LoginModel
    {
        public int BranchId { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
