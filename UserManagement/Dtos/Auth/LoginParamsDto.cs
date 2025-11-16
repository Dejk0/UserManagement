using System;
using System.Collections.Generic;
using System.Text;

namespace UserManagement.Dtos.Auth
{
    public class LoginParamsDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
