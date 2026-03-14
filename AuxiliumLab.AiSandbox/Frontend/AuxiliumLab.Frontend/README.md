# AuxiliumLab.Frontend

Blazor WebAssembly (.NET 10) single-page application for AuxiliumLab AI Sandbox.

## Stack

- **Framework:** .NET 10, Blazor WebAssembly
- **UI:** MudBlazor 8.0.0
- **Charts:** Blazor-ApexCharts 2.0.0
- **Real-time:** Microsoft.AspNetCore.SignalR.Client 9.0.4

## Feature pages

| Route | Page |
|---|---|
| `/training/ppo` | PPO Training form |
| `/training/status` | Training job status table |
| `/simulation/multiple-run` | Mass simulation run form |
| `/simulation/visualization` | Live SVG grid visualization (SignalR) |
| `/simulation/status` | Simulation job status table |
| `/aggregation/run` | Aggregation run step builder |
| `/aggregation/status` | Aggregation job status table |
| `/data-management/visualization/aggregation-run` | ApexCharts aggregation results (default page) |
| `/data-management/visualization/multiple-run` | ApexCharts simulation win/loss charts |
| `/data-management/visualization/training` | Placeholder |
| `/data-management/raw` | Placeholder |
| `/sandbox/settings` | API endpoint info |

## SignalR

The visualization page connects to `/hubs/simulation` on the backend. Events:

- `SimulationStarted` — grid dimensions, initial cells
- `AgentMoved` — agent position update
- `AgentToggled` — agent action (run/sprint/toggle)
- `TurnCompleted` — per-turn cell updates
- `SimulationEnded` — outcome + final turn

## Notifications

A global 50-entry notification log is available via the bell icon in the top-right app bar. Notifications are also emitted by every page when jobs start or stop.

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

The default route `/` redirects to `/data-management/visualization/aggregation-run`.

## Configuration

Edit `wwwroot/appsettings.json` to set API base URLs:

```json
{
  "ApiSettings": {
    "AiSandboxBaseUrl": "http://localhost:5000",
    "MarketSimulationBaseUrl": "http://localhost:6000"
  }
}
```

When running inside Docker the file is overwritten at container start by `entrypoint.sh`.  
Override the backend URL with the `AISANDBOX_BASE_URL` environment variable (see [`../Dockerfile`](../Dockerfile)).

## Tests

Unit tests live in [`../AuxiliumLab.AiSandbox.Frontend.UnitTests/`](../AuxiliumLab.AiSandbox.Frontend.UnitTests/README.md).  
Framework: MSTest + bUnit + FluentAssertions + Moq.
