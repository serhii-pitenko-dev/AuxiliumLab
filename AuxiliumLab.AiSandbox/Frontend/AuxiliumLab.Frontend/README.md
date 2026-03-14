# AuxiliumLab.Frontend

Blazor WebAssembly (.NET 10) single-page application for AuxiliumLab AI Sandbox.

## Stack

- **Framework:** .NET 10, Blazor WebAssembly
- **UI:** MudBlazor 7.17.0
- **Charts:** Blazor-ApexCharts 1.10.0
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
