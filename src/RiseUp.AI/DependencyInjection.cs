using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RiseUp.AI.Sandbox;
using RiseUp.Entities.Abstractions;

namespace RiseUp.AI;

public static class DependencyInjection
{
    public static IServiceCollection AddAi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AiOptions>(configuration.GetSection("Ai"));
        services.AddSingleton<IContentSafetyModel, SandboxContentSafetyModel>();
        services.AddSingleton<IAiMatchingService, SandboxAiMatchingService>();
        return services;
    }
}
