# AuxiliumLab.AiSandbox.Infrastructure

**Onion layer: Infrastructure**  
Implements the persistence ports (interfaces) defined in the Application layer.  
Depends on: `Domain`, `SharedBaseTypes`, `Statistics`.

## Purpose
Provides concrete implementations of data access abstractions used by the Application layer.  
Application Services are coded against interfaces only — Infrastructure is the plug.

## Components

### `IFileDataManager<T>` / `FileDataManager<T>`
Generic file-system repository.

| Method | Description |
|---|---|
| `SaveAsync(id, obj)` | Serializes `obj` to JSON and writes to `<path>/<id>.json` |
| `LoadObjectAsync(id)` | Reads and deserializes a JSON file by ID |
| `DeleteAsync(id)` | Removes the file |
| `GetAvailableIds()` | Lists all IDs (file names) in the storage folder |

**Storage path** is resolved from `InfrastructureSettings.FilesPath` (configurable in `appsettings.json`).  
Serialization uses `System.Text.Json` with custom converters registered in `Converters/`.

#### `NullFileDataManager<T>`
A no-op implementation used when file saving is disabled (e.g., training mode where I/O would bottleneck parallel runs). Registered in DI when `SaveToFile = false`.

### `IMemoryDataManager<T>` / `MemoryDataManager<T>`
In-memory concurrent dictionary.

| Method | Description |
|---|---|
| `AddOrUpdate(id, obj)` | Upserts by Guid key |
| `LoadObject(id)` | Returns stored object or throws `KeyNotFoundException` |
| `DeleteObject(id)` | Removes by key |
| `GetAvailableVersions()` | Returns all stored keys |
| `Clear()` | Wipes all entries |

Used to share `StandardPlayground` instances between the executor and the gRPC `SimulationService` within the same process.  
`MemoryDataManager<T>` is registered as a **singleton** so all consumers share the same instance.

### `Converters/`
Custom `System.Text.Json` converters for domain types that require non-default serialization (e.g., `Coordinates`, `Cell`, `SandboxMapBaseObject` polymorphic hierarchy).

### `Configuration/`
- `InfrastructureSettings` — file storage path, flags.
- `Preconditions/SandBoxConfiguration` — strongly-typed map configuration (size, turn limits, element percentages, incremental ranges).
- `InfrastructureServiceCollectionExtensions` — registers all infrastructure services.

## Dependency Inversion

```
ApplicationServices defines:           Infrastructure implements:
────────────────────────────────        ──────────────────────────────────
IFileDataManager<T>          ◄───────   FileDataManager<T>
IMemoryDataManager<T>        ◄───────   MemoryDataManager<T>
```

## Adding a New Storage Backend
1. Implement `IFileDataManager<T>` (or create a new port interface in `ApplicationServices`).
2. Register the new implementation in `InfrastructureServiceCollectionExtensions`.
3. Example: swapping to SQL would only require a new implementation here — Application code stays unchanged.

## Configuration Reference (`appsettings.json` → `SandBox` section)

| Key | Type | Description |
|---|---|---|
| `SaveToFileRegularity` | `int` | Save state every N turns (0 = disabled) |
| `TurnTimeout` | `int` (ms) | Max time per turn before timeout |
| `MaxTurns` | `IncrementalValue` | Turn limit with min/current/max/step for sweeps |
| `MapSettings.Size` | `MapSizeSettings` | Width/Height ranges and step |
| `MapSettings.FileSource` | `FileSourceSettings` | Load map from file instead of random generation |
| `MapSettings.ElementsPercentages` | `ElementsPercentages` | Block/enemy density |
