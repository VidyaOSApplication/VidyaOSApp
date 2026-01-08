
using Microsoft.EntityFrameworkCore;
using VidyaOSDAL.Models;
using VidyaOSHelper;
using VidyaOSServices.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;


namespace VidyaOSWebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddDbContext<VidyaOsContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));
            builder.Services.AddScoped<StudentService>();
            builder.Services.AddScoped<SchoolService>();
            builder.Services.AddScoped<TeacherService>();
            builder.Services.AddScoped<VidyaOSService>();
            builder.Services.AddScoped<VidyaOsContext>();
            builder.Services.AddScoped<TeacherHelper>();
            builder.Services.AddScoped<AuthHelper>();
            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<CommonService>();
            builder.Services.AddScoped<VidyaOSHelper.SchoolHelper.SchoolHelper>();
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            )
        };
    });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
