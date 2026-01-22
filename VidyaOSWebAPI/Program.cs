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

            // 🔥 REQUIRED FOR RENDER (dynamic PORT)
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

            // JWT Authentication
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

            // 🔥 CORS FOR NETLIFY + LOCAL DEV
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy
                        .WithOrigins(
                            "http://localhost:8100",        // Ionic local
                            "http://localhost:4200",        // Angular local
                            "https://your-app.netlify.app"  // 🔥 Netlify PROD (CHANGE THIS)
                        )
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            // Swagger (enable for prod also)
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

            // Swagger for DEV + PROD (safe for APIs)
            if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            // 🔥 CORS BEFORE AUTH
            app.UseCors("AllowFrontend");

            // 🔥 HANDLE PREFLIGHT (OPTIONS) REQUESTS
            app.Use(async (context, next) =>
            {
                if (context.Request.Method == HttpMethods.Options)
                {
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    return;
                }
                await next();
            });

            // AUTH
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
