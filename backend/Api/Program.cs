using System.Text.Json.Serialization;
using Api;
using Api.Common.Infrastructure;
using Api.Common.Options;
using DotNetEnv;
using FastEndpoints;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// other environments should not contain .env files
if (builder.Environment.IsDevelopment())
{
    Env.Load(".env");
}

builder.Configuration
    .AddEnvironmentVariables()
    .Build();

builder.Services
    .ConfigureAndValidate<PostgresOptions>()
    .AddScoped<IStartupService, MigrateDb>()
    .AddDbContext<AppDbContext>()
    .AddFastEndpoints()
    .AddOpenApi()
    .ConfigureHttpJsonOptions(
        options => { options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()); });

builder.Host
    .UseSerilog(
        (_, _, loggerConfiguration) =>
        {
            loggerConfiguration
                .Enrich.FromLogContext()
                .WriteTo.Console();
        }
    );

var app = builder.Build();

using (var startupScope = app.Services.CreateScope())
{
    var startupServices = startupScope.ServiceProvider.GetServices<IStartupService>();
    await Task.WhenAll(startupServices.Select(service => service.Run()));
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app
    .UseHttpsRedirection()
    .UseFastEndpoints();

await app.RunAsync();