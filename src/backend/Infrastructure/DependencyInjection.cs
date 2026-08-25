using Application.Abstractions.ConnectStone;
using Application.Abstractions.Persistence;
using Application.Abstractions.Realtime;
using ConnectStone.Sdk;
using ConnectStone.Sdk.Webhooks;
using Infrastructure.ConnectStone;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Realtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("Sqlite") ?? "Data Source=connectstone-demo.db"));

        services.AddScoped<IDemoOrderRepository, DemoOrderRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IConnectStoneGateway, ConnectStoneGateway>();
        services.AddScoped<IOrderStatusNotifier, SignalROrderStatusNotifier>();

        services.AddSignalR();

        services.AddConnectStoneClient(configuration);
        services.AddConnectStoneWebhooks(configuration);

        return services;
    }
}
