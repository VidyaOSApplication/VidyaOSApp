using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VidyaOSDAL.Models;

namespace VidyaOSHelper.SchoolHelper
{
    public class SchoolHelper
    {
        private readonly VidyaOsContext _context;
        public SchoolHelper(VidyaOsContext context)
        {
            _context = context;
        }
        public class ApiResult<T>
        {
            public bool Success { get; set; }
            public string Message { get; set; } = "";
            public T? Data { get; set; }

            public static ApiResult<T> Ok(T data, string message = "")
                => new() { Success = true, Data = data, Message = message };

            public static ApiResult<T> Fail(string message)
                => new() { Success = false, Message = message };
        }

    }
}
