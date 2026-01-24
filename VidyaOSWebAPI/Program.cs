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

            builder.Services.AddControllers();

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

                        RoleClaimType = ClaimTypes.Role,
                        NameClaimType = ClaimTypes.NameIdentifier
                    };
                });

            builder.Services.AddAuthorization();

            // 🔥 FINALIZED CORS POLICY FOR MOBILE & WEB
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontendApps", policy =>
                {
                    policy
                        .WithOrigins(
                            "http://localhost:8100",   // Ionic Browser
                            "http://localhost:4200",   // Angular Browser
                            "https://localhost",      // Capacitor Android/iOS (High Priority)
                            "http://localhost",       // Capacitor Android (Fallback)
                            "http://localhost:8081",   // Expo Web
                            "http://localhost:19006"   // Expo older web port
                        )
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials(); // Required if you use Cookies/Sessions
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
            builder.Services.AddScoped<VidyaOSHelper.SchoolHelper.SchoolHelper>();

            var app = builder.Build();

            // -------------------- MIDDLEWARE --------------------

            // Swagger should be available in Prod for your testing if needed, 
            // otherwise wrap in if (app.Environment.IsDevelopment())
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