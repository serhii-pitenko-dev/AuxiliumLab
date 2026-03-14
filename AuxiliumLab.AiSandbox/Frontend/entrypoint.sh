#!/bin/sh
# Inject the backend API URL into the Blazor WASM runtime configuration.
# The file is evaluated by the browser, so the URL must be reachable from
# the user's machine — not from inside the container network.
#
# Override at runtime:
#   docker run -e AISANDBOX_BASE_URL=http://my-host:5000 frontend
cat > /app/wwwroot/appsettings.json <<EOF
{
  "ApiSettings": {
    "AiSandboxBaseUrl": "${AISANDBOX_BASE_URL:-http://localhost:5000}",
    "MarketSimulationBaseUrl": "${MARKET_SIMULATION_BASE_URL:-http://localhost:6000}"
  }
}
EOF

exec dotnet /app/StaticHost.dll
