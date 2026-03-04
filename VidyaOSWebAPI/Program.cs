using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using VidyaOSDAL.Models;
using VidyaOSHelper;
using VidyaOSServices.Services;
using QuestPDF.Infrastructure;
using Microsoft.OpenApi.Models; // Added for Swagger Types

namespace VidyaOSWebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // -------------------- SERVICES --------------------
            builder.Services.AddControllers();

            builder.Services.AddDbContext<VidyaOsContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
            );

            // JWT Authentication Setup
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

            // CORS Policy
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontendApps", policy =>
                {
                    policy.WithOrigins(
                            "http://vidyaos.online", "https://vidyaos.online",
                            "http://www.vidyaos.online", "https://www.vidyaos.online",
                            "http://localhost:8100", "http://localhost:4200",
                            "http://localhost:8081", "http://localhost:19006"
                        )
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            // -------------------- SWAGGER SETUP --------------------
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "VidyaOS Web API", Version = "v1" });

                // This enables the "Authorize" button in Swagger
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // Dependency Injection - Standard Services
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
            builder.Services.AddScoped<AIChatService>();
            builder.Services.AddHttpClient<AIChatService>();
            

            builder.Services.AddScoped<VidyaOSHelper.SchoolHelper.SchoolHelper>();

            // ✅ Correct Typed Client Registration
          

            QuestPDF.Settings.License = LicenseType.Community;

            var app = builder.Build();

            // -------------------- MIDDLEWARE --------------------
            // Enable Swagger for both Development and Production so you can test on Azure
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "VidyaOS API v1");
                c.RoutePrefix = string.Empty; // Sets Swagger as the default home page
            });

            app.UseHttpsRedirection();
            app.UseRouting();

            // 🔥 Order is Critical: CORS must be before Auth
            app.UseCors("AllowFrontendApps");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.Run();
        }
    }
}