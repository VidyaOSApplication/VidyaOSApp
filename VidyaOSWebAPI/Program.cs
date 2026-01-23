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

            // -------------------- SERVICES --------------------

            // Controllers
            builder.Services.AddControllers();

            // DbContext
            builder.Services.AddDbContext<VidyaOsContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection")
                )
            );

            // JWT Authentication
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

                        // 🔥 REQUIRED FOR ROLE-BASED AUTH
                        RoleClaimType = ClaimTypes.Role,
                        NameClaimType = ClaimTypes.NameIdentifier
                    };
                });

            // 🔥 REQUIRED for [Authorize(Roles = "...")]
            builder.Services.AddAuthorization();

            // CORS (Ionic / Angular)
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowIonicApp", policy =>
                {
                    policy
                        .WithOrigins(
                            "http://localhost:8100", // Ionic
                            "http://localhost:4200"  // Angular (optional)
                        )
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            // Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Dependency Injection
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

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            // 🔥 CORS MUST RUN BEFORE AUTH
            app.UseCors("AllowIonicApp");

            // 🔥 ALLOW PREFLIGHT (OPTIONS) REQUESTS — PRODUCTION SAFE
            app.Use(async (context, next) =>
            {
                if (context.Request.Method == HttpMethods.Options)
                {
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    return;
                }
                await next();
            });

            // 🔥 AUTHENTICATION FIRST
            app.UseAuthentication();

            // 🔥 AUTHORIZATION SECOND
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
