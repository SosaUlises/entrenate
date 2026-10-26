namespace Entrenate.Api.Configuration
{
    public static class CorsConfiguration
    {
        public const string PolicyName = "FrontendOrigins";

        public static IServiceCollection AddConfiguredCors(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            var allowedOrigins = configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>()
                ?.Where(origin => !string.IsNullOrWhiteSpace(origin))
                .Select(origin => origin.Trim().TrimEnd('/'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
                ?? [];

            ValidateOrigins(allowedOrigins, environment);

            services.AddCors(options =>
            {
                options.AddPolicy(
                    PolicyName,
                    policy => policy
                        .WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod());
            });

            return services;
        }

        private static void ValidateOrigins(
            IReadOnlyCollection<string> origins,
            IHostEnvironment environment)
        {
            if (environment.IsProduction() && origins.Count == 0)
            {
                throw new InvalidOperationException(
                    "Cors:AllowedOrigins requiere al menos un origen en Production.");
            }

            foreach (var origin in origins)
            {
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
                    !string.IsNullOrEmpty(uri.PathAndQuery.Trim('/')) ||
                    !string.IsNullOrEmpty(uri.Fragment) ||
                    uri.Scheme is not ("http" or "https"))
                {
                    throw new InvalidOperationException(
                        $"El origen CORS '{origin}' no es una URL de origen válida.");
                }

                if (environment.IsProduction() &&
                    uri.Scheme != Uri.UriSchemeHttps)
                {
                    throw new InvalidOperationException(
                        "Todos los orígenes CORS deben usar HTTPS en Production.");
                }
            }
        }
    }
}
