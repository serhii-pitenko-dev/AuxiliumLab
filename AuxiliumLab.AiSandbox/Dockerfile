# ──────────────────────────────────────────────────────────────────────────────
# Stage 1 — restore
#   Copy only .csproj files so this layer is rebuilt only when dependencies
#   change, not on every source-code edit.
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS restore
WORKDIR /src

COPY Directory.Build.props ./

# Production projects
COPY AuxiliumLab.AiSandbox.Ai/AuxiliumLab.AiSandbox.Ai.csproj                                                     AuxiliumLab.AiSandbox.Ai/
COPY AuxiliumLab.AiSandbox.AiTrainingOrchestrator/AuxiliumLab.AiSandbox.AiTrainingOrchestrator.csproj             AuxiliumLab.AiSandbox.AiTrainingOrchestrator/
COPY AuxiliumLab.AiSandbox.ApplicationServices/AuxiliumLab.AiSandbox.ApplicationServices.csproj                   AuxiliumLab.AiSandbox.ApplicationServices/
COPY AuxiliumLab.AiSandbox.Common/AuxiliumLab.AiSandbox.Common.csproj                                             AuxiliumLab.AiSandbox.Common/
COPY AuxiliumLab.AiSandbox.ConsolePresentation/AuxiliumLab.AiSandbox.ConsolePresentation.csproj                   AuxiliumLab.AiSandbox.ConsolePresentation/
COPY AuxiliumLab.AiSandbox.Domain/AuxiliumLab.AiSandbox.Domain.csproj                                             AuxiliumLab.AiSandbox.Domain/
COPY AuxiliumLab.AiSandbox.GrpcHost/AuxiliumLab.AiSandbox.GrpcHost.csproj                                         AuxiliumLab.AiSandbox.GrpcHost/
COPY AuxiliumLab.AiSandbox.Infrastructure/AuxiliumLab.AiSandbox.Infrastructure.csproj                             AuxiliumLab.AiSandbox.Infrastructure/
COPY AuxiliumLab.AiSandbox.SharedBaseTypes/AuxiliumLab.AiSandbox.SharedBaseTypes.csproj                           AuxiliumLab.AiSandbox.SharedBaseTypes/
COPY AuxiliumLab.AiSandbox.Startup/AuxiliumLab.AiSandbox.Startup.csproj                                           AuxiliumLab.AiSandbox.Startup/
COPY AuxiliumLab.AiSandbox.WebApi/AuxiliumLab.AiSandbox.WebApi.csproj                                             AuxiliumLab.AiSandbox.WebApi/
COPY AuxiliumLab.Statistics/AuxiliumLab.AiSandbox.Statistics.csproj                                               AuxiliumLab.Statistics/

# Test project (unit tests only — integration tests are not run inside the container)
COPY AuxiliumLab.AiSandbox.UnitTests/AuxiliumLab.AiSandbox.UnitTests.csproj                                       AuxiliumLab.AiSandbox.UnitTests/

RUN dotnet restore AuxiliumLab.AiSandbox.Startup/AuxiliumLab.AiSandbox.Startup.csproj
RUN dotnet restore AuxiliumLab.AiSandbox.UnitTests/AuxiliumLab.AiSandbox.UnitTests.csproj

# ──────────────────────────────────────────────────────────────────────────────
# Stage 2 — build
#   Compile both the application and the unit-test project.
# ──────────────────────────────────────────────────────────────────────────────
FROM restore AS build
COPY . .

RUN dotnet build AuxiliumLab.AiSandbox.Startup/AuxiliumLab.AiSandbox.Startup.csproj \
        -c Release --no-restore

RUN dotnet build AuxiliumLab.AiSandbox.UnitTests/AuxiliumLab.AiSandbox.UnitTests.csproj \
        -c Release --no-restore

# ──────────────────────────────────────────────────────────────────────────────
# Stage 3 — test
#   Run the MSTest unit-test suite.  A failing test aborts the build so no
#   broken runtime image is ever produced.
#   Integration tests are excluded here because they require the live Python
#   gRPC service; run them separately with docker compose.
# ──────────────────────────────────────────────────────────────────────────────
FROM build AS test

RUN dotnet test AuxiliumLab.AiSandbox.UnitTests/AuxiliumLab.AiSandbox.UnitTests.csproj \
        -c Release --no-build --logger "console;verbosity=normal"

# ──────────────────────────────────────────────────────────────────────────────
# Stage 4 — publish
#   Self-contained publish: everything the runtime needs is included.
# ──────────────────────────────────────────────────────────────────────────────
FROM test AS publish

RUN dotnet publish AuxiliumLab.AiSandbox.Startup/AuxiliumLab.AiSandbox.Startup.csproj \
        -c Release --no-build -o /app/publish

# ──────────────────────────────────────────────────────────────────────────────
# Stage 5 — runtime
#   Lean ASP.NET Core runtime image.  Source code and SDK are not included.
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=publish /app/publish .

# Mount point for persistent file storage (maps, stats, trained models).
# Override FileSource__FileStorage__BasePath if the volume is mounted elsewhere.
VOLUME ["/file-storage"]

# 5000  — REST API   (HTTP/1.1)  consumed by the Blazor frontend and external clients
# 50062 — gRPC gym   (HTTP/2)    called back by the Python SB3 service during training
EXPOSE 5000
EXPOSE 50062

ENTRYPOINT ["dotnet", "AuxiliumLab.AiSandbox.Startup.dll"]
