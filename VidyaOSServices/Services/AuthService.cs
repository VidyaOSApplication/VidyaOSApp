using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VidyaOSDAL.DTOs;
using VidyaOSDAL.Models;
using VidyaOSHelper;
using static VidyaOSHelper.SchoolHelper.SchoolHelper;

namespace VidyaOSServices.Services
{
    public class AuthService
    {
        private readonly VidyaOsContext _context;
        private readonly AuthHelper _authHelper;
        public AuthService(VidyaOsContext vidyaOsContext, AuthHelper authHelper)
        {
            _context = vidyaOsContext;
            _authHelper = authHelper;
        }

        public async Task<ApiResult<LoginResponse>> LoginAsync(LoginRequest req)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Username == req.Username && u.IsActive == true);

            if (user == null)
                return ApiResult<LoginResponse>.Fail("Invalid username or password.");

            if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
                return ApiResult<LoginResponse>.Fail("Invalid username or password.");

            string token = _authHelper.GenerateJwt(user);

            return ApiResult<LoginResponse>.Ok(
                new LoginResponse
                {
                    Token = token,
                    Role = user.Role!,
                    ExpiresIn = 3600
                },
                "Logged in successfully."
            );
        }
    }
}
