# AuxiliumLab.AiSandbox.Statistics (AuxiliumLab.Statistics)

**Onion layer: Infrastructure**  
Persistence and reporting layer for simulation run data.  
Depends on: `SharedBaseTypes`, `AuxiliumLab.AiSandbox.Common`.

> **Folder:** `AuxiliumLab.Statistics/` (project name: `AuxiliumLab.AiSandbox.Statistics`)

## Purpose
Records outcomes from batch simulation runs, exports them to CSV, and provides the data structures used by `MassRunner` to summarise and compare runs across different configurations.

## Folder Structure
```
AuxiliumLab.Statistics/
├── Converters/         TableConverter — converts summary objects to CSV rows
├── Preconditions/      Settings objects for incremental sweep configuration
├── Result/             Domain result DTOs (shared with ApplicationServices.Domain.Statistics)
└── StatisticDataManager/ IStatisticFileDataManager, StatisticFileDataManager
```

## Key Types

### Result DTOs (`Result/`)

| Class | Description |
|---|---|
| `ParticularRun` | Outcome of a single simulation: playground ID, turns, enemy count, win/loss reason |
| `BatchSummary` | Aggregated stats for a batch: total runs, wins, average turns, batch ID |
| `IncrementalRunSummary` | Summary for one step of an incremental sweep: property name, step value, nested `BatchSummary` list |
| `MassRunSummary` | Top-level summary of a full `MassRunner` execution: batch count, elapsed time, swept properties |
| `GeneralBatchRunInformation` | Metadata about the batch: timestamp, configuration snapshot, map settings |
| `SandboxRunResult` | Low-level per-run data used before aggregation |

### `IStatisticFileDataManager` / `StatisticFileDataManager`
Specialised file manager for statistics output.

| Method | Description |
|---|---|
| `AppendDataToFileAsync(fileName, data)` | Serialises `data` as a JSON line and appends it to a file in the `STATISTICS/` folder |
| `ConvertToCsvAndAppendAsync(fileName, csvContent)` | Appends a pre-formatted CSV string to a file in the `STATISTICS/` folder |
| `SaveAggregationReportAsync(steps, runDate)` | Builds the full multi-section aggregation CSV report from `AggregationStepResult[]` and saves it |

Aggregation report files are named with a timestamp: `aggregation_{yyyy-MM-dd_HH-mm-ss}.csv`.  
Individual mass-run data files use caller-supplied names (typically a GUID or descriptive identifier).

### `TableConverter`
A `static` utility class that maps result objects to ordered string arrays (CSV rows).  
Used by `AggregationReportConverter` and `MassRunner`; `StatisticFileDataManager` itself does not call `TableConverter` directly.

### Preconditions (`Preconditions/`)

| Class | Description |
|---|---|
| `SimulationStartupSettings` | Top-level sweep settings: which properties to sweep and how many simulations per step |
| `SimulationIncrementalPropertiesSettings` | List of `RangeSettings` for each property to sweep |
| `RangeSettings` | `PropertyName`, `Min`, `Max`, `Step` |
| `SimulationSandBoxSettings` | Fork of `SandBoxConfiguration` used during incremental runs to override individual values |

## Output Files
`StatisticFileDataManager` writes all output to `{FileStorage.BasePath}/STATISTICS/`:
```
D:\FILE_STORAGE\STATISTICS\
└── aggregation_{yyyy-MM-dd_HH-mm-ss}.csv    ← full multi-section aggregation report
    <caller-supplied-name>.json              ← individual JSON lines (appended mass-run data)
```

## Adding a New Statistic Column
1. Add the property to the relevant Result DTO (`ParticularRun`, `BatchSummary`, etc.).
2. Update `TableConverter` to include the new value in the row array.
3. Update `MassRunner` to populate the new property when constructing the DTO.

## Adding a New Incremental Property
1. Add the property name constant to `IncrementalPropertyNames` (in `ApplicationServices/Runner/MassRunner/`).
2. Update `MassRunner.RunIncrementalSweepPhaseAsync` to handle the new property name by overriding the appropriate value in the cloned `SandBoxConfiguration`.
