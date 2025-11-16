using System;
using System.Collections.Generic;
using System.Text;

namespace UserManagement.Dtos.Auth
{
    public class RegistrationResultDto : BaseValidResponse
    {
        public bool Success { get; set; }
        public string? CallbackUrl { get; set; }
        public string? Error { get; set; }
    }
}
