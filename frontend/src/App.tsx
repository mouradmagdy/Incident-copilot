import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  fetchLatestMetrics,
  fetchRecentLogs,
  fetchServices,
  fetchSimulationScenario,
  setSimulationScenario,
  type LogEvent,
  type Service,
  type ServiceMetric,
  type SimulationScenario,
} from './api'
import './App.css'

const serviceLimit = 4

const scenarios: Array<{ value: SimulationScenario; label: string }> = [
  { value: 'Healthy', label: 'Healthy' },
  { value: 'DatabaseConnectionExhaustion', label: 'DB connection exhaustion' },
]

function formatNumber(value: number, maximumFractionDigits = 0) {
  return new Intl.NumberFormat('en-US', { maximumFractionDigits }).format(value)
}

function formatTime(value: string) {
  return new Intl.DateTimeFormat('en-US', {
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  }).format(new Date(value))
}

function getLogLevel(level: number | string) {
  if (typeof level === 'string') return level

  return ['Debug', 'Information', 'Warning', 'Error', 'Critical'][level] ?? 'Unknown'
}

function getServiceState(metric: ServiceMetric | null | undefined) {
  if (!metric) return { label: 'Awaiting data', tone: 'muted' }

  const isDegraded =
    metric.p95LatencyMs >= 1_000 ||
    metric.errorRatePercent >= 5 ||
    metric.activeDbConnections >= 90

  return isDegraded
    ? { label: 'Degraded', tone: 'danger' }
    : { label: 'Operational', tone: 'healthy' }
}

function Metric({ label, value }: { label: string; value: string }) {
  return (
    <div className="metric">
      <dt>{label}</dt>
      <dd>{value}</dd>
    </div>
  )
}

function ServiceCard({
  service,
  metric,
}: {
  service: Service
  metric: ServiceMetric | null | undefined
}) {
  const state = getServiceState(metric)

  return (
    <article className="service-card">
      <div className="service-card__header">
        <div>
          <p className="eyebrow">Service</p>
          <h2>{service.name}</h2>
        </div>
        <span className={`status status--${state.tone}`}>
          <span className="status__dot" aria-hidden="true" />
          {state.label}
        </span>
      </div>

      <dl className="metric-grid">
        <Metric
          label="P95 latency"
          value={metric ? `${formatNumber(metric.p95LatencyMs)} ms` : '—'}
        />
        <Metric
          label="Error rate"
          value={metric ? `${formatNumber(metric.errorRatePercent, 1)}%` : '—'}
        />
        <Metric
          label="CPU"
          value={metric ? `${formatNumber(metric.cpuUsagePercent)}%` : '—'}
        />
        <Metric
          label="DB connections"
          value={metric ? formatNumber(metric.activeDbConnections) : '—'}
        />
        <Metric
          label="Queue depth"
          value={metric ? formatNumber(metric.queueDepth) : '—'}
        />
      </dl>

      <p className="sample-time">
        {metric ? `Sampled ${formatTime(metric.timestampUtc)}` : 'Waiting for first sample'}
      </p>
    </article>
  )
}

function LogsTable({ logs }: { logs: LogEvent[] }) {
  if (logs.length === 0) {
    return <div className="empty-state">No recent log events.</div>
  }

  return (
    <div className="log-table-wrap">
      <table className="log-table">
        <thead>
          <tr>
            <th>Time</th>
            <th>Service</th>
            <th>Level</th>
            <th>Message</th>
            <th>Trace</th>
          </tr>
        </thead>
        <tbody>
          {logs.map((log) => {
            const level = getLogLevel(log.level)
            return (
              <tr key={`${log.serviceId}-${log.id}`}>
                <td className="log-time">{formatTime(log.timestampUtc)}</td>
                <td>{log.serviceName}</td>
                <td>
                  <span className={`log-level log-level--${level.toLowerCase()}`}>
                    {level}
                  </span>
                </td>
                <td className="log-message">{log.message}</td>
                <td className="trace-id" title={log.traceId ?? undefined}>
                  {log.traceId?.slice(0, 8) ?? '—'}
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}

function App() {
  const queryClient = useQueryClient()

  const servicesQuery = useQuery({
    queryKey: ['services'],
    queryFn: fetchServices,
    staleTime: 60_000,
  })

  const services = (servicesQuery.data ?? []).slice(0, serviceLimit)
  const serviceIds = services.map((service) => service.id)

  const metricsQuery = useQuery({
    queryKey: ['latest-metrics', serviceIds],
    queryFn: ({ signal }) => fetchLatestMetrics(serviceIds, signal),
    enabled: serviceIds.length > 0,
    refetchInterval: 10_000,
  })

  const logsQuery = useQuery({
    queryKey: ['recent-logs', serviceIds],
    queryFn: ({ signal }) => fetchRecentLogs(services, signal),
    enabled: serviceIds.length > 0,
    refetchInterval: 10_000,
  })

  const scenarioQuery = useQuery({
    queryKey: ['simulation-scenario'],
    queryFn: fetchSimulationScenario,
    retry: false,
  })

  const scenarioMutation = useMutation({
    mutationFn: setSimulationScenario,
    onSuccess: (scenario) => {
      queryClient.setQueryData(['simulation-scenario'], scenario)
      void queryClient.invalidateQueries({ queryKey: ['latest-metrics'] })
      void queryClient.invalidateQueries({ queryKey: ['recent-logs'] })
    },
  })

  const hasError = servicesQuery.isError || metricsQuery.isError || logsQuery.isError
  const isRefreshing = metricsQuery.isFetching || logsQuery.isFetching
  const updatedAt = Math.max(metricsQuery.dataUpdatedAt, logsQuery.dataUpdatedAt)

  return (
    <main className="dashboard-shell">
      <header className="topbar">
        <div className="brand">
          <span className="brand-mark" aria-hidden="true">IC</span>
          <div>
            <p className="eyebrow">Incident Copilot</p>
            <h1>Operations overview</h1>
          </div>
        </div>

        <div className="live-state" aria-live="polite">
          <span className={`live-state__dot ${hasError ? 'live-state__dot--error' : ''}`} />
          <div>
            <strong>{hasError ? 'Connection issue' : isRefreshing ? 'Refreshing' : 'Live'}</strong>
            <span>{updatedAt ? `Updated ${formatTime(new Date(updatedAt).toISOString())}` : 'Connecting to API'}</span>
          </div>
        </div>
      </header>

      {hasError && (
        <div className="error-banner" role="alert">
          The dashboard could not reach the Incident Copilot API. Confirm the API is running on port 5087.
        </div>
      )}

      {scenarioQuery.isSuccess && (
        <section className="scenario-control" aria-labelledby="scenario-title">
          <div>
            <p className="eyebrow">Development simulation</p>
            <h2 id="scenario-title">Active scenario</h2>
          </div>

          <div className="scenario-actions">
            <div className="scenario-switch" role="group" aria-label="Simulation scenario">
              {scenarios.map((scenario) => {
                const isActive = scenarioQuery.data.scenario === scenario.value

                return (
                  <button
                    key={scenario.value}
                    className={`scenario-button ${isActive ? 'scenario-button--active' : ''}`}
                    type="button"
                    aria-pressed={isActive}
                    disabled={scenarioMutation.isPending}
                    onClick={() => scenarioMutation.mutate(scenario.value)}
                  >
                    {scenario.label}
                  </button>
                )
              })}
            </div>

            <span className="scenario-feedback" aria-live="polite">
              {scenarioMutation.isPending
                ? 'Switching scenario…'
                : scenarioMutation.isError
                  ? 'Could not switch scenario.'
                  : 'Changes appear on the next telemetry sample.'}
            </span>
          </div>
        </section>
      )}

      <section aria-labelledby="services-title">
        <div className="section-heading">
          <div>
            <p className="eyebrow">Environment health</p>
            <h2 id="services-title">Monitored services</h2>
          </div>
          <span className="polling-note">Polling every 10 seconds</span>
        </div>

        <div className="service-grid">
          {servicesQuery.isPending
            ? Array.from({ length: serviceLimit }, (_, index) => (
                <div className="service-card service-card--loading" key={index} />
              ))
            : services.map((service) => (
                <ServiceCard
                  key={service.id}
                  service={service}
                  metric={metricsQuery.data?.[service.id]}
                />
              ))}
        </div>
      </section>

      <section className="logs-section" aria-labelledby="logs-title">
        <div className="section-heading">
          <div>
            <p className="eyebrow">Latest signals</p>
            <h2 id="logs-title">Recent logs</h2>
          </div>
          <span className="result-count">{logsQuery.data?.length ?? 0} events</span>
        </div>

        {logsQuery.isPending && serviceIds.length > 0
          ? <div className="empty-state">Loading recent logs…</div>
          : <LogsTable logs={logsQuery.data ?? []} />}
      </section>
    </main>
  )
}

export default App
