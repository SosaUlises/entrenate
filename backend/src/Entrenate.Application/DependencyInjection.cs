using Entrenate.Application.Common.Behaviors;
using FluentValidation;
using Entrenate.Application.Coach.RoutinePlanner.Proposal;
using Microsoft.Extensions.DependencyInjection;

namespace Entrenate.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(
            this IServiceCollection services)
        {
            services.AddMediatR(configuration =>
            {
                configuration.RegisterServicesFromAssembly(
                    typeof(DependencyInjection).Assembly);

                configuration.AddOpenBehavior(
                    typeof(ValidationBehavior<,>));
            });

            services.AddValidatorsFromAssembly(
                typeof(DependencyInjection).Assembly);

            services.AddScoped<RoutinePlannerProposalGenerator>();

            return services;
        }
    }
}
