using System;

namespace HISWEBAPI.Models
{
    public class LoginModel
    {
        public int Id { get; set; }
        public int HospId { get; set; }

        public string FirstName { get; set; }
        public string MidelName { get; set; }
        public string LastName { get; set; }

        public string Gender { get; set; }
        public string Email { get; set; }
        public string Contact { get; set; }
        public string Address { get; set; }

        public DateTime? DOB { get; set; }

        public string UserName { get; set; }
        public int isSuperAdmin { get; set; }
        public string Password { get; set; }
        public int IsActive { get; set; }

        public DateTime? LoginOn { get; set; }
        public string Name { get; set; } // This seems like branch or role name
        public int RoleId { get; set; }
    }
}
