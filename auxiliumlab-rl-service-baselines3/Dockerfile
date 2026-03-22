# ──────────────────────────────────────────────────────────────────────────────
# Stage 1 – deps
#   Install heavy Python dependencies (torch, grpcio, etc.) into a dedicated
#   virtual-env layer so the final image can be rebuilt quickly when only
#   source code changes.
# ──────────────────────────────────────────────────────────────────────────────
FROM python:3.11-slim AS deps

# System libraries required by grpcio / torch / matplotlib
RUN apt-get update && apt-get install -y --no-install-recommends \
        build-essential \
        libgomp1 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /install

COPY requirements.txt .

# Install CPU-only PyTorch first to avoid pulling in the large CUDA variant
# that the default PyPI wheel includes.  The subsequent full requirements
# install will see the pinned torch already satisfied and skip it.
RUN pip install --no-cache-dir \
        torch==2.10.0 \
        --index-url https://download.pytorch.org/whl/cpu

RUN pip install --no-cache-dir -r requirements.txt

# ──────────────────────────────────────────────────────────────────────────────
# Stage 2 – test
#   Copy source and run the full pytest suite.  If any test fails the Docker
#   build fails here and no deployable image is produced.
# ──────────────────────────────────────────────────────────────────────────────
FROM deps AS test

WORKDIR /app

# Copy source (generated protobuf stubs must be present before tests run)
COPY . .

# The gRPC-generated stubs use flat imports; make the generated/ dir visible.
ENV PYTHONPATH=/app/generated

# Run unit / integration tests – build fails fast if they are red
RUN python -m pytest tests/ -v --tb=short

# ──────────────────────────────────────────────────────────────────────────────
# Stage 3 – runtime
#   Lean production image that starts the gRPC server.
# ──────────────────────────────────────────────────────────────────────────────
FROM deps AS runtime

WORKDIR /app

COPY . .

# ── Ports ─────────────────────────────────────────────────────────────────────
# 50051 – PolicyTrainer gRPC (called by the .NET AiSandbox)
EXPOSE 50051

# ── Persistent data directories ───────────────────────────────────────────────
# Mount host paths here so checkpoints and trained models survive container
# restarts.  docker-compose.yml maps volumes onto these paths.
VOLUME ["/app/checkpoints", "/app/trained_models"]

# ── Runtime environment defaults ──────────────────────────────────────────────
ENV GRPC_HOST=0.0.0.0 \
    GRPC_PORT=50051 \
    GRPC_MAX_WORKERS=10 \
    LOG_LEVEL=INFO \
    CHECKPOINT_DIR=/app/checkpoints \
    MODELS_DIR=/app/trained_models \
    # Override this when the .NET host is not on the same machine.
    # Inside Docker on Windows/Mac use host.docker.internal.
    # Inside Docker on Linux with host networking use the host IP.
    SIMULATION_GRPC_HOST=host.docker.internal:50062

# Run as non-root for security
RUN adduser --disabled-password --gecos "" appuser \
    && mkdir -p /app/checkpoints /app/trained_models /file-storage \
    && chown -R appuser /app /file-storage
USER appuser

HEALTHCHECK --interval=15s --timeout=5s --start-period=30s --retries=3 \
    CMD python healthcheck.py || exit 1

CMD ["python", "server.py"]
