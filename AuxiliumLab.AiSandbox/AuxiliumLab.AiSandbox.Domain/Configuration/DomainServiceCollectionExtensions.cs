using AuxiliumLab.AiSandbox.Domain.Agents.Factories;
using AuxiliumLab.AiSandbox.Domain.Agents.Services.Vision;
using AuxiliumLab.AiSandbox.Domain.Playgrounds.Builders;
using AuxiliumLab.AiSandbox.Domain.Playgrounds.Factories;
using Microsoft.Extensions.DependencyInjection;

namespace AuxiliumLab.AiSandbox.Domain.Configuration;

public static class DomainServiceCollectionExtensions
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddTransient<IPlaygroundBuilder, PlaygroundBuilder>();
        services.AddTransient<IPlaygroundFactory, PlaygroundFactory>();

        services.AddTransient<IEnemyFactory, EnemyFactory>();
        services.AddTransient<IHeroFactory, HeroFactory>();
        services.AddTransient<IVisibilityService, VisibilityService>();

        return services;
    }
}

