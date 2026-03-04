using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VidyaOSDAL.DTOs
{
    public class AIQueryRequest
    {
        public string Table { get; set; } = "";
        public string Operation { get; set; } = "SELECT"; // SELECT, COUNT, SUM
        public List<string>? Columns { get; set; }
        public List<AIQueryFilter>? Filters { get; set; }
        public List<string>? Joins { get; set; } // Optional related tables
    }

    public class AIQueryFilter
    {
        public string Column { get; set; } = "";
        public string Operator { get; set; } = "=";
        public string Value { get; set; } = "";
    }
}
