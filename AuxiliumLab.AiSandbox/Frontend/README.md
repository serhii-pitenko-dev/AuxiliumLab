# Frontend

This folder contains the Blazor WebAssembly frontend for AuxiliumLab AI Sandbox.

## Projects

| Project | Description |
|---|---|
| `AuxiliumLab.Frontend` | Blazor WebAssembly (.NET 10) application |
| `AuxiliumLab.AiSandbox.Frontend.UnitTests` | bUnit + MSTest unit tests |

## Running the frontend

```bash
cd AuxiliumLab.Frontend
dotnet run
```

The app starts at `https://localhost:7001` (or `http://localhost:5001`) and connects to the backend at the URL configured in `wwwroot/appsettings.json`.

## Configuration

Edit `wwwroot/appsettings.json` to set API endpoints:

```json
{
  "ApiSettings": {
    "AiSandboxBaseUrl": "http://localhost:5000",
    "MarketSimulationBaseUrl": "http://localhost:6000"
  }
}
```

## Architecture

Feature-based layout: each menu section has its own folder under `Features/`.

```
Features/
  Training/         – PPO training form, training status table
  Simulation/       – Mass run form, SVG visualization with SignalR, status table
  AggregationRun/   – Aggregation step builder, status table
  Statistics/       – Data clients for completed run summaries
  DataManagement/   – ApexCharts visualizations for completed runs
  Sandbox/          – Settings page
Shared/             – Reusable Blazor components (SandboxSettingsForm, PageHeader)
Services/           – INotificationService, IApiContextProvider, MenuService
Http/               – ApiClientBase
Configuration/      – ApiSettings
```

## Default page

The default route `/` redirects to `/data-management/visualization/aggregation-run`.
