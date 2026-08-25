using Application.Abstractions.ConnectStone;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Api.IntegrationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"connectstone-tests-{Guid.NewGuid():N}.db");

    public FakeConnectStoneGateway Gateway { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Sqlite"] = $"Data Source={_dbPath}",
                ["ConnectStone:SecretKey"] = "sk_test_dummy",
                ["ConnectStone:ServiceRefererName"] = "integration-tests",
                ["ConnectStone:Webhook:Username"] = "hookuser",
                ["ConnectStone:Webhook:Password"] = "hookpass",
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IConnectStoneGateway>();
            services.AddSingleton<IConnectStoneGateway>(Gateway);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            foreach (var suffix in new[] { "", "-wal", "-shm" })
            {
                File.Delete(_dbPath + suffix);
            }
        }
    }
}
