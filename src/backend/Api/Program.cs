using Api;
using Api.Endpoints;
using Application.Abstractions.Behaviors;
using FluentValidation;
using Infrastructure;
using Infrastructure.Persistence;
using Infrastructure.Realtime;
using Mediator;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console(builder.Environment.IsDevelopment()
            ? new Serilog.Formatting.Display.MessageTemplateTextFormatter(
                "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            : new Serilog.Formatting.Compact.CompactJsonFormatter())
        .CreateLogger();

    builder.Host.UseSerilog();

    builder.Services.AddMediator(options =>
    {
        options.Assemblies = [typeof(Application.AssemblyReference)];
        options.PipelineBehaviors = [typeof(ValidationBehavior<,>)];
        options.ServiceLifetime = ServiceLifetime.Scoped;
    });
    builder.Services.AddValidatorsFromAssembly(typeof(Application.AssemblyReference).Assembly);
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddOpenApi();

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails(options =>
    {
        options.CustomizeProblemDetails = context =>
        {
            if (!builder.Environment.IsDevelopment())
            {
                return;
            }

            var exception = context.HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
            if (exception is not null)
            {
                context.ProblemDetails.Extensions["exception"] = exception.ToString();
            }
        };
    });

    builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>(tags: ["ready"]);

    // The Angular dev server (ng serve, http://localhost:4200) is a different origin from
    // `dotnet run`'s Kestrel port during local development. In the docker-compose deployment, both
    // sit behind the same reverse-proxy origin instead, so this policy simply never matters there.
    const string DevCorsPolicy = "DevCors";
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(DevCorsPolicy, policy => policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
    });

    var app = builder.Build();

    using (var migrationScope = app.Services.CreateScope())
    {
        var dbContext = migrationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
        app.UseCors(DevCorsPolicy);
    }

    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    });

    app.UseSerilogRequestLogging(options =>
    {
        options.GetLevel = (httpContext, _, exception) => exception is not null
            ? LogEventLevel.Error
            : httpContext.Request.Path.StartsWithSegments("/alive") || httpContext.Request.Path.StartsWithSegments("/ready")
                ? LogEventLevel.Verbose
                : LogEventLevel.Information;
    });

    app.UseExceptionHandler();

    app.MapHealthChecks("/alive", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
    app.MapHealthChecks("/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") }).AllowAnonymous();

    app.MapOrderEndpoints();
    app.MapWebhookEndpoints();
    app.MapDemoEndpoints();
    app.MapHub<OrderStatusHub>("/hubs/order-status");

    app.Run();
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    Log.Fatal(exception, "Connect Stone demo API terminated unexpectedly during startup");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
