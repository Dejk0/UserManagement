using System;
using System.Collections.Generic;
using System.Text;

namespace UserManagement
{
    public class BaseValidResponse
    {
        public bool IsValid { get; set; }
        public string[] Message { get; set; }
    }
}
