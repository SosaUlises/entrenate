using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.Common.Settings;
using Entrenate.Infrastructure.Authentication;
using Entrenate.Infrastructure.Coach.Conversation;
using Entrenate.Infrastructure.Email;
using Entrenate.Infrastructure.Identity;
using Entrenate.Infrastructure.Persistence;
using Entrenate.Infrastructure.Persistence.Repositories;
using Entrenate.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Net.Http.Headers;
using System.Text;


namespace Entrenate.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            // PostgreSQL
            var connectionString =
                configuration.GetConnectionString("PostgreConnectionString")
                ?? throw new InvalidOperationException(
                    "No se encontró la connection string PostgreConnectionString.");

            ValidateDatabaseTransport(
                connectionString,
                environment);

            services.AddDbContext<EntrenateDbContext>(options =>
            {
                options.UseNpgsql(connectionString);
            });


            // Data Protection
            var dataProtection = services
                .AddDataProtection()
                .SetApplicationName("Entrenate");
            var dataProtectionKeysPath =
                configuration["DataProtection:KeysPath"];

            if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
            {
                if (environment.IsProduction() &&
                    !Path.IsPathFullyQualified(dataProtectionKeysPath))
                {
                    throw new InvalidOperationException(
                        "DataProtection:KeysPath debe ser una ruta absoluta " +
                        "en Production.");
                }

                Directory.CreateDirectory(dataProtectionKeysPath);
                dataProtection.PersistKeysToFileSystem(
                    new DirectoryInfo(dataProtectionKeysPath));
            }


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
            services
                .AddOptions<JwtSettings>()
                .Bind(configuration.GetSection("Jwt"))
                .Validate(
                    settings =>
                        Encoding.UTF8.GetByteCount(settings.Key) >= 32,
                    "Jwt:Key debe tener al menos 32 bytes.")
                .Validate(
                    settings =>
                        !string.IsNullOrWhiteSpace(settings.Issuer),
                    "Jwt:Issuer es obligatorio.")
                .Validate(
                    settings =>
                        !string.IsNullOrWhiteSpace(settings.Audience),
                    "Jwt:Audience es obligatorio.")
                .Validate(
                    settings =>
                        settings.ExpirationMinutes is > 0 and <= 1440,
                    "Jwt:ExpirationMinutes debe estar entre 1 y 1440.")
                .ValidateOnStart();

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

            // Email Brevo
            services
                .AddOptions<EmailSettings>()
                .Bind(configuration.GetSection("Email"))
                .Validate(
                    settings =>
                        !string.IsNullOrWhiteSpace(settings.ApiKey),
                    "Email:ApiKey es obligatorio.")
                .Validate(
                    settings =>
                        !string.IsNullOrWhiteSpace(settings.FromEmail),
                    "Email:FromEmail es obligatorio.")
                .Validate(
                    settings =>
                        !string.IsNullOrWhiteSpace(settings.FromName),
                    "Email:FromName es obligatorio.")
                .ValidateOnStart();

            // Frontend
            services
                .AddOptions<FrontendSettings>()
                .Bind(configuration.GetSection("Frontend"))
                .Validate(
                    settings => !string.IsNullOrWhiteSpace(settings.BaseUrl),
                    "Frontend:BaseUrl es obligatorio.")
                .ValidateOnStart();

            // Groq Coach Conversation
            services
                .AddOptions<GroqSettings>()
                .Bind(configuration.GetSection("Groq"))
                .Validate(
                    settings =>
                        Uri.TryCreate(
                            settings.BaseUrl,
                            UriKind.Absolute,
                            out var baseUri) &&
                        baseUri.Scheme == Uri.UriSchemeHttps,
                    "Groq:BaseUrl debe ser una URL HTTPS absoluta.")
                .Validate(
                    settings => !string.IsNullOrWhiteSpace(settings.Model),
                    "Groq:Model es obligatorio.")
                .Validate(
                    settings => settings.MaxOutputTokens is > 0 and <= 4096,
                    "Groq:MaxOutputTokens debe estar entre 1 y 4096.")
                .Validate(
                    settings => settings.TimeoutSeconds is > 0 and <= 300,
                    "Groq:TimeoutSeconds debe estar entre 1 y 300.")
                .Validate(
                    settings => !string.IsNullOrWhiteSpace(settings.ApiKey),
                    "Groq:ApiKey es obligatorio.");

            // Authorization
            services.AddAuthorization();


            // Infrastructure Services
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IIdentityService, IdentityService>();
            services.AddHttpClient<IEmailService, BrevoEmailService>();
            services.AddHttpClient<
                ICoachConversationProvider,
                GroqCoachConversationProvider>((serviceProvider, client) =>
                {
                    var settings = serviceProvider
                        .GetRequiredService<
                            Microsoft.Extensions.Options.IOptions<GroqSettings>>()
                        .Value;

                    client.BaseAddress = new Uri(
                        settings.BaseUrl.TrimEnd('/') + "/");
                    client.Timeout = TimeSpan.FromSeconds(
                        settings.TimeoutSeconds);
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue(
                            "Bearer",
                            settings.ApiKey);
                });
            services.AddScoped<DatabaseSeeder>();

            services.AddScoped<ITrainingProfileRepository,TrainingProfileRepository>();
            services.AddScoped<IEquipmentRepository, EquipmentRepository>();
            services.AddScoped<IExerciseRepository, ExerciseRepository>();
            services.AddScoped<IRoutineRepository, RoutineRepository>();
            services.AddScoped<ITrainingSessionRepository, TrainingSessionRepository>();



            return services;
        }

        private static void ValidateDatabaseTransport(
            string connectionString,
            IHostEnvironment environment)
        {
            if (!environment.IsProduction())
            {
                return;
            }

            NpgsqlConnectionStringBuilder builder;

            try
            {
                builder = new NpgsqlConnectionStringBuilder(
                    connectionString);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException(
                    "La connection string PostgreConnectionString no es válida.",
                    exception);
            }

            if (builder.SslMode is
                SslMode.Disable or
                SslMode.Allow or
                SslMode.Prefer)
            {
                throw new InvalidOperationException(
                    "PostgreConnectionString debe exigir SSL/TLS en Production.");
            }
        }
    }
}
