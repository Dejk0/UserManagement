using System;
using System.Collections.Generic;
using System.Text;

namespace UserManagement.Dtos.Auth
{
    public class RegistrationParamDto 
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
    }
}
