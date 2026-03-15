using EpicRPGBot.Mcp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();

builder.Services.AddSingleton<RepositoryPaths>();
builder.Services.AddSingleton<ArtifactStore>();
builder.Services.AddSingleton<UiBuildArtifactService>();
builder.Services.AddSingleton<WindowHandleResolver>();
builder.Services.AddSingleton<AutomationElementInspector>();
builder.Services.AddSingleton<UiAppSession>();
builder.Services.AddSingleton<UiAutomationFacade>();
builder.Services.AddSingleton<DevToolsProtocolClient>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
