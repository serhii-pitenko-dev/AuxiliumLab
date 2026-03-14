# Developer Setup — AuxiliumLab

Complete guide for setting up both the .NET simulation engine and the Python RL training service on a new machine.

---

## Prerequisites

| Tool | Minimum version | Purpose |
|---|---|---|
| .NET SDK | 9.0 | Build and run the C# solution |
| Python | 3.10+ | RL training service |
| PowerShell | 5.1 / pwsh 7+ | Build scripts |

---

## Repository Structure

```
AuxiliumLab/
├── AuxiliumLab.AiSandbox/               .NET 9 simulation engine + REST API + gRPC host
│   ├── Frontend/                         Blazor WebAssembly frontend (.NET 10)
│   ├── docs/                             Architecture documentation
│   └── AuxiliumLab.AiSandbox.sln
└── auxiliumlab-rl-service-baselines3/    Python gRPC RL training service (SB3)
```

---

## 1. Python RL Service Setup

```powershell
cd auxiliumlab-rl-service-baselines3

# Create and activate virtual environment
python -m venv .venv
.\.venv\Scripts\Activate.ps1

# Install dependencies
pip install -r requirements.txt

# Generate gRPC stubs — only needed if a .proto file changed
.\scripts\generate_all_grpc.ps1
```

> The generated files in `generated/` are already committed. Only regenerate when `proto/` files change.

---

## 2. .NET Solution Setup

```powershell
cd AuxiliumLab.AiSandbox

# Build everything
dotnet build AuxiliumLab.AiSandbox.sln
```

### Configuration

The main configuration file is `AuxiliumLab.AiSandbox.Startup/appsettings.json`.  
Review these keys before the first run:

| Key | Default | Notes |
|---|---|---|
| `PolicyTrainerClient.ServerAddress` | `http://localhost:50051` | Must match the Python service port |
| `StartupSettings.IsWebApiEnabled` | — | Set `true` to start the REST API on `:5000` |
| `StartupSettings.IsPreconditionStart` | `true` | `true` = skip interactive menu, use file settings directly |
| `InfrastructureSettings.FilesPath` | (path) | Root folder for all file output (maps, stats, models) |

---

## 3. Running with Docker Compose (recommended)

All three services can be built and started with a single command from the workspace root:

```powershell
# Build images (first run or after code changes)
docker compose build

# Start all services
docker compose up -d

# Check status
docker compose ps

# View logs
docker compose logs -f

# Stop all services
docker compose down
```

| URL | Service |
|---|---|
| `http://localhost:5000/swagger` | Backend REST API + Swagger UI |
| `http://localhost:8080` | Blazor WebAssembly frontend |
| `localhost:50051` | Python gRPC (PolicyTrainer, internal) |

**Environment variables** can be overridden at startup:
```powershell
# Point the frontend at a non-default backend
$env:AISANDBOX_BASE_URL = "http://my-backend:5000"
docker compose up -d
```

> **Docker Hub note:** Due to a TLS certificate issue with Cloudflare CDN, all images use `mcr.microsoft.com` only. The frontend is served by a minimal ASP.NET Core `StaticHost` instead of nginx.

---

## 4. Running Both Services Manually

> **Boot order matters for training:** start the Python service first. It must be fully running before `Training` mode is started from .NET.

### Step 1 — Start the Python gRPC service (terminal A)

```powershell
cd auxiliumlab-rl-service-baselines3
.\.venv\Scripts\Activate.ps1
python server.py
# INFO - gRPC server started on 0.0.0.0:50051
```

### Step 2 — Run the .NET simulation engine (terminal B)

```powershell
cd AuxiliumLab.AiSandbox
dotnet run --project AuxiliumLab.AiSandbox.Startup
# Interactive menu appears (or runs directly when IsPreconditionStart = true)
```

### Step 3 — (Optional) Run the Blazor frontend (terminal C)

```powershell
cd AuxiliumLab.AiSandbox/Frontend/AuxiliumLab.Frontend
dotnet run
# https://localhost:7001  (or http://localhost:5001)
```

Edit `Frontend/AuxiliumLab.Frontend/wwwroot/appsettings.json` → `ApiSettings.AiSandboxBaseUrl`  
to point at the .NET REST API (`http://localhost:5000` by default).

---

## 5. Port Reference

| Port | Service | Notes |
|---|---|---|
| `:50051` | Python `PolicyTrainerService` (gRPC) | .NET → Python: start training, get status, act |
| `:50062` | .NET `SimulationService` (gRPC) | Python → .NET: gym reset / step (training only) |
| `:5000` | .NET REST API | Active when `IsWebApiEnabled = true` |
| `:7001` / `:5001` | Blazor frontend (dev) | HTTPS / HTTP |
| `:8080` | Frontend Docker container | Served by `StaticHost` (mapped from `:80` inside container) |

---

## 6. Running Tests

### .NET unit tests

```powershell
cd AuxiliumLab.AiSandbox
dotnet test AuxiliumLab.AiSandbox.UnitTests\AuxiliumLab.AiSandbox.UnitTests.csproj
```

### .NET integration tests

> Training tests require the Python service to be running on `:50051`.  
> All other tests (simulation, statistic, aggregation status) run without it.

```powershell
cd AuxiliumLab.AiSandbox
dotnet test AuxiliumLab.AiSandbox.IntegrationTests\AuxiliumLab.AiSandbox.IntegrationTests.csproj
```

### Python tests

```powershell
cd auxiliumlab-rl-service-baselines3
.\.venv\Scripts\Activate.ps1
pytest -v
```

### Blazor frontend tests

```powershell
cd AuxiliumLab.AiSandbox/Frontend/AuxiliumLab.AiSandbox.Frontend.UnitTests
dotnet test
```

---

## 7. Generating gRPC Stubs

Stubs are auto-generated from `.proto` files. Only regenerate after a proto change.

### C# stubs (MSBuild auto-generates on every build)

```powershell
cd AuxiliumLab.AiSandbox
dotnet build   # <Protobuf> items in each .csproj trigger stub generation
```

### Python stubs

```powershell
cd auxiliumlab-rl-service-baselines3
.\.venv\Scripts\Activate.ps1
.\scripts\generate_all_grpc.ps1

# Or manually:
python -m grpc_tools.protoc `
  -I./proto `
  --python_out=./generated `
  --grpc_python_out=./generated `
  proto/policy_trainer.proto
```

> Never edit files in `generated/` by hand — they are overwritten on regeneration.

---

## 8. Further Reading

| Document | Contents |
|---|---|
| [AuxiliumLab.AiSandbox/README.md](AuxiliumLab.AiSandbox/README.md) | Solution overview, execution modes, REST API reference |
| [AuxiliumLab.AiSandbox/docs/ARCHITECTURE.md](AuxiliumLab.AiSandbox/docs/ARCHITECTURE.md) | Onion architecture, dependency graph, data flow diagrams |
| [auxiliumlab-rl-service-baselines3/README.md](auxiliumlab-rl-service-baselines3/README.md) | Python service architecture, algorithms, API usage |
| [AI_GUIDELINES.md](AI_GUIDELINES.md) | Repository rules for AI-assisted development |
