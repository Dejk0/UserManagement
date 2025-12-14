using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Text;

namespace UserManagement.Dtos.Auth
{
    public class EnginsViewAccessDto : BaseValidResponse
    {
        public bool[]? MotorokViewAccess { get; set; }
    }
}
