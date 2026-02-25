using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using VidyaOSDAL.Models;
using VidyaOSHelper;
using VidyaOSServices.Services;
using QuestPDF.Infrastructure;

namespace VidyaOSWebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // -------------------- SERVICES --------------------

            builder.Services.AddControllers();

            // ✅ UPDATED — SQL RETRY ADDED (NO FUNCTIONALITY CHANGE)
            builder.Services.AddDbContext<VidyaOsContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,                     // retry attempts
                            maxRetryDelay: TimeSpan.FromSeconds(10), // delay between retry
                            errorNumbersToAdd: null
                        );
                    }
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

                        RoleClaimType = ClaimTypes.Role,
                        NameClaimType = ClaimTypes.NameIdentifier
                    };
                });

            builder.Services.AddAuthorization();

            // 🔥 UPDATED CORS POLICY FOR LIVE NETLIFY DEPLOYMENT
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontendApps", policy =>
                {
                    policy
                        .WithOrigins(
                            "http://vidyaos.online",
                            "https://vidyaos.online",
                            "http://www.vidyaos.online",
                            "https://www.vidyaos.online",
                            "http://localhost:8100",
                            "http://localhost:4200",
                            "https://localhost",
                            "http://localhost",
                            "http://localhost:8081",
                            "http://localhost:19006"
                        )
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Enter: Bearer {your JWT token}"
                });

                options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });

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
            builder.Services.AddScoped<SubscriptionService>();
            builder.Services.AddScoped<VidyaOSHelper.SchoolHelper.SchoolHelper>();

            QuestPDF.Settings.License = LicenseType.Community;

            var app = builder.Build();

            // -------------------- MIDDLEWARE --------------------

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseHttpsRedirection();

            app.UseRouting();

            // 🔥 CORS MUST COME BEFORE AUTHENTICATION
            app.UseCors("AllowFrontendApps");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}