using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using VidyaOSDAL.Models;
using VidyaOSHelper;
using VidyaOSServices.Services;

namespace VidyaOSWebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 🔥 REQUIRED FOR RENDER (Dynamic PORT)
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(
                    int.Parse(Environment.GetEnvironmentVariable("PORT") ?? "8080")
                );
            });

            // -------------------- SERVICES --------------------

            builder.Services.AddControllers();

            // Database
            builder.Services.AddDbContext<VidyaOsContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection")
                )
            );

            // -------------------- JWT AUTH --------------------

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
                        ),

                        RoleClaimType = ClaimTypes.Role,
                        NameClaimType = ClaimTypes.NameIdentifier
                    };
                });

            builder.Services.AddAuthorization();

            // -------------------- CORS --------------------

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy
                        .WithOrigins(
                            "http://localhost:8100",           // Ionic local
                            "http://localhost:4200",           // Angular local
                            "https://vidyaosapp.netlify.app"   // Production frontend
                        )
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            // Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // -------------------- DEPENDENCY INJECTION --------------------

            builder.Services.AddScoped<StudentService>();
            builder.Services.AddScoped<SchoolService>();
            builder.Services.AddScoped<TeacherService>();
            builder.Services.AddScoped<VidyaOSService>();
            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<AuthHelper>();
            builder.Services.AddScoped<CommonService>();
            builder.Services.AddScoped<StudentHelper>();
            builder.Services.AddScoped<TeacherHelper>();
            builder.Services.AddScoped<ExamService>();
            builder.Services.AddScoped<VidyaOSHelper.SchoolHelper.SchoolHelper>();

            var app = builder.Build();

            // -------------------- MIDDLEWARE --------------------

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseHttpsRedirection();

            // 🔥 VERY IMPORTANT: CORS BEFORE AUTH
            app.UseCors("AllowFrontend");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
