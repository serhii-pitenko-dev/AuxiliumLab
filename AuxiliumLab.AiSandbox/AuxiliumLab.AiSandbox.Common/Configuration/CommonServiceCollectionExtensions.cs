using AuxiliumLab.AiSandbox.Common.MessageBroker;
using Microsoft.Extensions.DependencyInjection;

namespace AuxiliumLab.AiSandbox.Common.Extensions;

public static class CommonServiceCollectionExtensions
{
    public static IServiceCollection AddEventAggregator(this IServiceCollection services)
    {
        services.AddSingleton<IMessageBroker, MessageBroker.MessageBroker>();
        services.AddSingleton<IBrokerRpcClient, BrokerRpcClient>();
        services.AddSingleton<GymBrokerRegistry>();

        return services;
    }
}