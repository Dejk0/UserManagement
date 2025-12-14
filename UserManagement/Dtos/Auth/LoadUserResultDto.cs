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
        public string[]? Roles { get; set; }
        public bool[]? Engines { get; set; }
        public int Tokens { get; set; }
        public bool HasEngine { get; set; }
    }
}
