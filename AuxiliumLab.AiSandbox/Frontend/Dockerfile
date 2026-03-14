# ──────────────────────────────────────────────────────────────────────────────
# Stage 1 — restore
#   Copy only .csproj files so this layer is rebuilt only when dependencies
#   change, not on every source-code edit.
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

COPY AuxiliumLab.Frontend/AuxiliumLab.Frontend.csproj                                                           AuxiliumLab.Frontend/
COPY AuxiliumLab.AiSandbox.Frontend.UnitTests/AuxiliumLab.AiSandbox.Frontend.UnitTests.csproj                   AuxiliumLab.AiSandbox.Frontend.UnitTests/
COPY StaticHost/StaticHost.csproj                                                                                StaticHost/

RUN dotnet restore AuxiliumLab.Frontend/AuxiliumLab.Frontend.csproj
RUN dotnet restore AuxiliumLab.AiSandbox.Frontend.UnitTests/AuxiliumLab.AiSandbox.Frontend.UnitTests.csproj
RUN dotnet restore StaticHost/StaticHost.csproj

# ──────────────────────────────────────────────────────────────────────────────
# Stage 2 — build
# ──────────────────────────────────────────────────────────────────────────────
FROM restore AS build
COPY . .

RUN dotnet build AuxiliumLab.Frontend/AuxiliumLab.Frontend.csproj \
        -c Release --no-restore

RUN dotnet build AuxiliumLab.AiSandbox.Frontend.UnitTests/AuxiliumLab.AiSandbox.Frontend.UnitTests.csproj \
        -c Release --no-restore

RUN dotnet build StaticHost/StaticHost.csproj \
        -c Release --no-restore

# ──────────────────────────────────────────────────────────────────────────────
# Stage 3 — test
#   Run bUnit + MSTest component tests.  A failing test aborts the build.
# ──────────────────────────────────────────────────────────────────────────────
FROM build AS test

RUN dotnet test AuxiliumLab.AiSandbox.Frontend.UnitTests/AuxiliumLab.AiSandbox.Frontend.UnitTests.csproj \
        -c Release --no-build --logger "console;verbosity=normal"

# ──────────────────────────────────────────────────────────────────────────────
# Stage 4 — publish
#   • Blazor WASM publishes its static files to /app/publish/wwwroot/
#   • StaticHost (minimal ASP.NET Core static-file server) publishes to /host/publish/
# ──────────────────────────────────────────────────────────────────────────────
FROM test AS publish

RUN dotnet publish AuxiliumLab.Frontend/AuxiliumLab.Frontend.csproj \
        -c Release --no-build -o /app/publish

RUN dotnet publish StaticHost/StaticHost.csproj \
        -c Release --no-build -o /host/publish

# ──────────────────────────────────────────────────────────────────────────────
# Stage 5 — runtime
#   StaticHost (ASP.NET Core) serves the Blazor WASM files on port 80.
#   Uses only mcr.microsoft.com images — no Docker Hub dependency.
#
#   The backend API URL (AISANDBOX_BASE_URL) is injected into
#   wwwroot/appsettings.json at container startup via entrypoint.sh so it can
#   be set per environment without rebuilding the image.
#
#   Default: http://localhost:5000
#   (works when docker-compose maps the AiSandbox REST port to the host)
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copy the StaticHost server binary (framework-dependent, aspnet:10.0 supplies the runtime)
COPY --from=publish /host/publish/ ./

# Place Blazor WASM static files where StaticHost's UseStaticFiles() expects them
COPY --from=publish /app/publish/wwwroot ./wwwroot/

COPY entrypoint.sh /entrypoint.sh
RUN  chmod +x /entrypoint.sh

EXPOSE 80

# Kestrel: listen on all interfaces, HTTP only (TLS terminates outside the container)
ENV ASPNETCORE_URLS=http://+:80

ENTRYPOINT ["/entrypoint.sh"]
