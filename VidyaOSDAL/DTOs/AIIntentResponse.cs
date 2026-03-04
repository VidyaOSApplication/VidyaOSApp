using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class AIIntentResponse
    {
        public string Intent { get; set; } = "";
        public Dictionary<string, string>? Parameters { get; set; }
    }
}
