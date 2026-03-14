# Frontend

Blazor WebAssembly frontend for AuxiliumLab AI Sandbox.

## Projects

| Project | Readme |
|---|---|
| `AuxiliumLab.Frontend` | [AuxiliumLab.Frontend/README.md](AuxiliumLab.Frontend/README.md) — SPA, feature pages, SignalR, MudBlazor |
| `AuxiliumLab.AiSandbox.Frontend.UnitTests` | [AuxiliumLab.AiSandbox.Frontend.UnitTests/README.md](AuxiliumLab.AiSandbox.Frontend.UnitTests/README.md) — bUnit + MSTest |

## Running

```bash
cd AuxiliumLab.Frontend
dotnet run
```

The app starts at `https://localhost:7001` (or `http://localhost:5001`).  
The backend REST API must be running on `http://localhost:5000` (configure in `AuxiliumLab.Frontend/wwwroot/appsettings.json`).

## Running with Docker

The entire stack (backend + frontend + Python RL service) can be started from the workspace root:

```powershell
docker compose up -d
```

The frontend container is served by `StaticHost` (a minimal ASP.NET Core static file server) and is accessible at `http://localhost:8080`.  
Set `AISANDBOX_BASE_URL` to override the backend URL at container start:

```powershell
$env:AISANDBOX_BASE_URL = "http://my-backend:5000"
docker compose up -d
```

See [`docker-compose.yml`](../../docker-compose.yml) and [`Dockerfile`](Dockerfile) at this folder level for details.
