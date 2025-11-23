using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Text;

namespace UserManagement.Dtos.Auth
{
    public class LoadUserResultDto
    {
        public string? Name { get; set; }
        public bool IsAuthenticated { get; set; }
    }
}
