# Quick Start Guide

## 5-Minute Setup

### 1. Create & Activate Virtual Environment
```powershell
python -m venv .venv
.\.venv\Scripts\Activate.ps1
```

### 2. Install Dependencies
```powershell
pip install -r requirements.txt
```

### 3. Generate gRPC Code
```powershell
pwsh -File Makefile.ps1 generate
```

### 4. Start the Server
```powershell
python server.py
```

You should see:
```
INFO - Starting RL Training Service...
INFO - gRPC server started on 0.0.0.0:50051
INFO - Server is ready to accept requests
```

### 5. Check Server Health (Optional)
```powershell
python healthcheck.py
```

## Testing

```powershell
# All tests
pytest -v

# With coverage
pytest --cov=auxilium_rl --cov-report=html -v
```

## Build Scripts

```powershell
pwsh -File Makefile.ps1                  # show available commands
pwsh -File Makefile.ps1 generate         # regenerate gRPC stubs
pwsh -File Makefile.ps1 test             # run tests
pwsh -File Makefile.ps1 serve            # start server
pwsh -File Makefile.ps1 clean            # clean build artifacts
```

## Project Structure

```
proto/                    # Protocol buffer definitions
generated/                # Auto-generated gRPC code (do not edit)
auxilium_rl/
  ├── transport/          # gRPC server & servicer
  ├── core/               # Business logic (algorithms, training, env)
  └── infra/              # Infrastructure (config, storage, adapters)
server.py                 # Main entry point
healthcheck.py            # Health check client
tests/                    # Unit and integration tests
```

## Troubleshooting

| Problem | Fix |
|---|---|
| `No module named 'generated'` | Run `pwsh -File Makefile.ps1 generate` |
| `Module 'grpc_tools' not found` | Activate venv and `pip install -r requirements.txt` |
| Server won't start on port 50051 | Check port availability or set `$env:GRPC_PORT = "50052"` |

See [README.md](README.md) for full documentation.
