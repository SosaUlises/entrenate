using Entrenate.Application.Coach.AdaptiveTraining.Proposal;
using Entrenate.Application.Coach.LiveCoach.Context;
using Entrenate.Application.Coach.LiveCoach.Prompt;
using Entrenate.Application.Coach.LiveCoach.Routing;
using Entrenate.Application.Coach.LiveCoach.Tools;
using Entrenate.Application.Coach.RoutinePlanner.Proposal;
using Entrenate.Application.Common.Behaviors;
using FluentValidation;
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
            services.AddScoped<SessionAdaptationProposalGenerator>();

            // Live Coach
            services.AddScoped<ILiveCoachContextBuilder, LiveCoachContextBuilder>();
            services.AddScoped<ILiveCoachToolRegistry, LiveCoachToolRegistry>();
            services.AddScoped<ILiveCoachIntentRouter, LiveCoachIntentRouter>();
            services.AddScoped<ILiveCoachPromptBuilder, LiveCoachPromptBuilder>();

            return services;
        }
    }
}
