using AuxiliumLab.Frontend.Configuration;
using AuxiliumLab.Frontend.Features.AggregationRun.Services;
using AuxiliumLab.Frontend.Features.Simulation.Services;
using AuxiliumLab.Frontend.Features.Statistics.Services;
using AuxiliumLab.Frontend.Features.Training.Services;
using AuxiliumLab.Frontend.Http;
using AuxiliumLab.Frontend.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<AuxiliumLab.Frontend.App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ── Configuration ─────────────────────────────────────────────────────────
var http = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
var defaultsBytes = await http.GetByteArrayAsync("default-values.json");
builder.Configuration.AddJsonStream(new MemoryStream(defaultsBytes));

builder.Services.Configure<ApiSettings>(
    builder.Configuration.GetSection("ApiSettings"));
builder.Services.Configure<SandboxSettings>(
    builder.Configuration.GetSection("SandboxParameters"));

// ── MudBlazor ─────────────────────────────────────────────────────────────
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.TopRight;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.VisibleStateDuration = 10000;
});

// ── Core services ─────────────────────────────────────────────────────────
builder.Services.AddSingleton<INotificationService, NotificationService>();
builder.Services.AddSingleton<IApiContextProvider, ApiContextProvider>();
// MenuService injects HttpClient which is Scoped — must be Scoped too.
builder.Services.AddScoped<IMenuService, MenuService>();

// ── HTTP client for JSON config loading (uses Blazor host base address) ───
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// ── Feature API clients ───────────────────────────────────────────────────
builder.Services.AddHttpClient<ITrainingApiClient, TrainingApiClient>((sp, client) =>
{
    var ctx = sp.GetRequiredService<IApiContextProvider>();
    client.BaseAddress = new Uri(ctx.AiSandboxBaseUrl);
});

builder.Services.AddHttpClient<ISimulationApiClient, SimulationApiClient>((sp, client) =>
{
    var ctx = sp.GetRequiredService<IApiContextProvider>();
    client.BaseAddress = new Uri(ctx.AiSandboxBaseUrl);
});

builder.Services.AddHttpClient<IAggregationApiClient, AggregationApiClient>((sp, client) =>
{
    var ctx = sp.GetRequiredService<IApiContextProvider>();
    client.BaseAddress = new Uri(ctx.AiSandboxBaseUrl);
});

builder.Services.AddHttpClient<IStatisticsApiClient, StatisticsApiClient>((sp, client) =>
{
    var ctx = sp.GetRequiredService<IApiContextProvider>();
    client.BaseAddress = new Uri(ctx.AiSandboxBaseUrl);
});

// ── SignalR Hub client (scoped so each page gets a fresh connection) ───────
builder.Services.AddScoped<ISimulationHubClient, SimulationHubClient>();

await builder.Build().RunAsync();
