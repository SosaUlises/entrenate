using Entrenate.Application.Common.Interfaces;
using Entrenate.Infrastructure.Authentication;
using Entrenate.Infrastructure.Identity;
using Entrenate.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;


namespace Entrenate.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // PostgreSQL
            var connectionString =
                configuration.GetConnectionString("PostgreConnectionString")
                ?? throw new InvalidOperationException(
                    "No se encontró la connection string PostgreConnectionString.");

            services.AddDbContext<EntrenateDbContext>(options =>
            {
                options.UseNpgsql(connectionString);
            });


            // Data Protection
            services.AddDataProtection();


            // ASP.NET Core Identity
            services
                .AddIdentityCore<ApplicationUser>(options =>
                {
                    options.User.RequireUniqueEmail = true;

                    options.Password.RequiredLength = 8;
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = false;
                })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<EntrenateDbContext>()
                .AddDefaultTokenProviders();


            // JWT Settings
            services.Configure<JwtSettings>(
                configuration.GetSection("Jwt"));

            var jwtSettings = configuration.GetSection("Jwt");

            var jwtKey = jwtSettings["Key"]
                ?? throw new InvalidOperationException(
                    "No se encontró la configuración Jwt:Key.");

            var jwtIssuer = jwtSettings["Issuer"]
                ?? throw new InvalidOperationException(
                    "No se encontró la configuración Jwt:Issuer.");

            var jwtAudience = jwtSettings["Audience"]
                ?? throw new InvalidOperationException(
                    "No se encontró la configuración Jwt:Audience.");


            // JWT Authentication
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme =
                        JwtBearerDefaults.AuthenticationScheme;

                    options.DefaultChallengeScheme =
                        JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters =
                        new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,

                            ValidIssuer = jwtIssuer,
                            ValidAudience = jwtAudience,

                            IssuerSigningKey =
                                new SymmetricSecurityKey(
                                    Encoding.UTF8.GetBytes(jwtKey)),

                            ClockSkew = TimeSpan.Zero
                        };
                });


            // Authorization
            services.AddAuthorization();


            // Infrastructure Services
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IIdentityService, IdentityService>();


            return services;
        }
    }
}
