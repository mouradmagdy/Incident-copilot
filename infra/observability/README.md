# Local observability

The observability stack is optional and runs behind the `observability` Docker
Compose profile:

```powershell
docker compose --profile observability up -d
```

This starts the application stack and the four observability services. To run
the API on the host instead, start the dependencies explicitly:

```powershell
docker compose --profile observability up -d postgres embedding-service tempo otel-collector prometheus grafana
dotnet run --project backend/IncidentCopilot.Api --launch-profile http
```

Data flows through the stack as follows:

```text
IncidentCopilot.Api
  -- OTLP/gRPC --> OpenTelemetry Collector
                       |-- traces  --> Tempo
                       `-- metrics --> Prometheus exporter <-- Prometheus scrapes

Grafana -- queries --> Tempo and Prometheus
```

Local endpoints:

- Grafana: http://localhost:3000 (`admin` / `admin`)
- Prometheus: http://localhost:9090
- Tempo API: http://localhost:3200
- OTLP/gRPC collector receiver: `localhost:4317`
- OTLP/HTTP collector receiver: `localhost:4318`

Generate API traffic, then open Grafana **Explore**. Select Tempo and use:

```traceql
{ resource.service.name = "IncidentCopilot.Api" }
```

Select Prometheus and search for metrics beginning with `incidentcopilot_`, or
verify incoming API traffic with:

```promql
http_server_request_duration_seconds_count{exported_job="IncidentCopilot.Api"}
```

The Collector's Prometheus exporter preserves its own scrape `job` label and
exposes OpenTelemetry's `service.name` as `exported_job`. The API exports
development metrics every 10 seconds. The observability services are not
started by a normal `docker compose up`; the `--profile observability` flag is
required.

Stop it with:

```powershell
docker compose --profile observability stop grafana prometheus otel-collector tempo
```

This leaves the application services and PostgreSQL container running.
