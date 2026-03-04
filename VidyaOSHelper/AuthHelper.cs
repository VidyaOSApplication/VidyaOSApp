using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using VidyaOSDAL.Models;

namespace VidyaOSHelper
{
    public class AuthHelper
    {
        private readonly IConfiguration _config;

        public AuthHelper(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateJwt(User user)
        {
            // 🚀 Safety Checks
            if (user == null)
                throw new Exception("User is null");

            if (user.UserId == null)
                throw new Exception("UserId is null");

            // 🚀 Claims Definition
            // These are stored inside the token and can be read by your React Native app
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Role, user.Role ?? ""),
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // Unique ID for this token
            };

            // ✅ Add SchoolId ONLY if present
            if (user.SchoolId.HasValue)
            {
                claims.Add(new Claim("schoolId", user.SchoolId.Value.ToString()));
            }

            // 🚀 Security Key Configuration
            // Ensure "Jwt:Key" in your appsettings is at least 32 characters long
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // 🚀 Token Creation
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                // 🕒 SECURE TIMING: 7 Days (UTC) is the best balance for School ERPs
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}