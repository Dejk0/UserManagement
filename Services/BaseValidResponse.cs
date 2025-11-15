using System;
using System.Collections.Generic;
using System.Text;

namespace Services
{
    public class BaseValidResponse
    {
        public bool IsValid { get; set; }
        public string[] Message { get; set; }
    }
}
