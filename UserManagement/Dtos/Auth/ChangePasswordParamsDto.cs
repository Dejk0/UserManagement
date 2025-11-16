using System;
using System.Collections.Generic;
using System.Text;

namespace UserManagement.Dtos.Auth
{
    public class ChangePasswordParamsDto
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }
}
