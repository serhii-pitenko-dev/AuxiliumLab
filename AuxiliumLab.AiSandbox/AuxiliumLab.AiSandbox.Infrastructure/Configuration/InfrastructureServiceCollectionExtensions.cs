using AuxiliumLab.AiSandbox.Domain.Playgrounds;
using AuxiliumLab.AiSandbox.Infrastructure.Configuration.Preconditions;
using AuxiliumLab.AiSandbox.Infrastructure.FileManager;
using AuxiliumLab.AiSandbox.Infrastructure.MemoryManager;
using AuxiliumLab.AiSandbox.SharedBaseTypes.AiContract.Dto;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuxiliumLab.AiSandbox.Infrastructure.Configuration;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SandBoxConfiguration>(configuration.GetSection("SandBox"));
        services.Configure<FileSourceConfiguration>(configuration.GetSection(FileSourceConfiguration.SectionName));
        services.Configure<AggregationSettings>(configuration.GetSection(AggregationSettings.SectionName));

        // Changed from Map to Sandbox
        services.AddSingleton<IMemoryDataManager<StandardPlayground>, MemoryDataManager<StandardPlayground>>();
        services.AddSingleton<IMemoryDataManager<AgentStateForAIDecision>, MemoryDataManager<AgentStateForAIDecision>>();

        // Add File Data Managers
        services.AddSingleton<IFileDataManager<StandardPlayground>, FileDataManager<StandardPlayground>>();
        services.AddSingleton<IFileDataManager<AgentStateForAIDecision>, FileDataManager<AgentStateForAIDecision>>();

        return services;
    }
}

