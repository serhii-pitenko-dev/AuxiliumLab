# AuxiliumLab.AiSandbox.GrpcHost

**Onion layer: Presentation / Host**  
ASP.NET Core gRPC server that exposes the C# simulation as a **Gymnasium-compatible environment** for the Python SB3 training service.  
Depends on: `Common`, `SharedBaseTypes`.

## Purpose
During AI training, the Python `ExternalSimEnv` needs to call `reset()` and `step()` on a live simulation. This project hosts those calls as gRPC endpoints.

The gRPC host listens on **port 50062** (Kestrel) and is started only when `ExecutionMode = Training`.

## Architecture

```
Python SB3 Service
  ExternalSimEnv
    └─ GrpcExternalEnvAdapter (localhost:50062)
         │                             .NET GrpcHost
         ├─ reset(gym_id, seed)  ──►  SimulationService.Reset()
         │                               └─ Publishes RequestSimulationResetCommand via IMessageBroker
         │                                    └─ TrainingExecutor handles → resets playground
         │                               ◄─ SimulationResetResponse → returns observation
         │
         └─ step(gym_id, action) ──►  SimulationService.Step()
                                          └─ Publishes RequestSimulationStepCommand via IMessageBroker
                                               └─ TrainingExecutor executes action → runs one turn
                                          ◄─ SimulationStepResponse → returns obs/reward/done/info
```

The `gym_id` (GUID) maps each Python gym instance to one `TrainingExecutor` running in the .NET process.

## Services

### `SimulationService` (gRPC)
Implements `SimulationServiceBase` (generated from `simulation.proto`).

| RPC | Description |
|---|---|
| `Reset(ResetRequest)` | Creates a new episode; returns initial observation |
| `Step(StepRequest)` | Executes one action; returns (observation, reward, terminated, truncated, info) |
| `Close(CloseRequest)` | Terminates the environment instance |

**Request/response bridging via MessageBroker:**  
Each RPC publishes a `Command` on `IMessageBroker` and then awaits a correlated `Response` using a `TaskCompletionSource`. The correlation is achieved by matching `GymId + CorrelationId` on the response. Timeout is 30 seconds.

## Proto Files
`Protos/simulation.proto` — defines the gym interface.  
Shared with the Python service (`auxiliumlab-rl-service-baselines3/proto/simulation.proto`).

Regenerate C# stubs:
```powershell
cd AuxiliumLab.AiSandbox
dotnet build   # csproj includes <Protobuf> items that auto-generate stubs
```

## Configuration
`appsettings.json` controls logging and Kestrel port.  
Port can be changed in `GrpcHost/Configuration/GrpcTrainingHost.cs`:
```csharp
options.ListenLocalhost(50062, o => o.Protocols = HttpProtocols.Http2);
```

## Integration with Startup
`GrpcTrainingHost` (in `Startup`) builds a `WebApplication` with Kestrel configured for HTTP/2.  
Core services are registered by `RegisterCoreServices()` in `Program.cs` and dedicated gRPC services are added via `AddGrpcHostServices()`.

## Testing
```powershell
# From Python service directory with .venv active:
python test_grpc_communication.py
```

Or using grpcurl:
```bash
grpcurl -plaintext localhost:50062 grpc.health.v1.Health/Check
```

## Project Structure
```
GrpcHost/
├── Protos/
│   └── simulation.proto              Service contract (gym interface)
├── Services/
│   └── SimulationService.cs          gRPC implementation, bridges to IMessageBroker
├── Configuration/
│   └── GrpcHostServiceCollectionExtensions.cs
├── Examples/
│   └── PolicyTrainerClientExample.cs Example of calling Python from .NET
├── Properties/
│   └── launchSettings.json
└── appsettings.json
```

## Next Steps
- ✅ gRPC infrastructure is set up and connected to `IMessageBroker`.
- ✅ `SimulationService` bridges Python gym calls to .NET training executors.
- ⏭️ Wire real reward shaping from domain win/loss conditions.
- ⏭️ Expand observation vector beyond basic `[x, y, enemies, turn]`.


See [GRPC_SETUP.md](../GRPC_SETUP.md) for complete documentation.
