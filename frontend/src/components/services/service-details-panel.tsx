import { Activity, Cpu, Database, Gauge, MemoryStick } from 'lucide-react'
import type { ReactNode } from 'react'
import {
  Area,
  AreaChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import type { Service, ServiceMetric } from '../../types/api'
import { formatNumber, formatTime } from '../../utils/formatters'
import { isMetricDegraded } from '../../utils/telemetry'
import { DetailPanel } from '../common/detail-panel'

export function ServiceDetailsPanel({
  service,
  metrics,
  onClose,
}: {
  service: Service
  metrics: ServiceMetric[]
  onClose: () => void
}) {
  const latest = metrics[0]
  const status = !service.isActive ? 'Down' : isMetricDegraded(latest) ? 'Degraded' : 'Healthy'
  const statusClass = status === 'Healthy'
    ? 'bg-success-soft text-success'
    : status === 'Degraded'
      ? 'bg-warning-soft text-warning'
      : 'bg-danger-soft text-danger'
  const chartData = metrics
    .slice(0, 48)
    .reverse()
    .map((metric) => ({
      label: formatTime(metric.timestampUtc),
      requests: metric.requestsPerSecond,
      latency: metric.p95LatencyMs,
    }))

  return (
    <DetailPanel
      title={service.name}
      subtitle={service.description}
      badge={<span className={`rounded-full px-2 py-1 text-[9px] font-semibold ${statusClass}`}>{status}</span>}
      onClose={onClose}
    >
      <div className="grid grid-cols-3 gap-2">
        <MetricTile label="Requests / sec" value={latest ? formatNumber(latest.requestsPerSecond) : '—'} />
        <MetricTile label="Error rate" value={latest ? `${formatNumber(latest.errorRatePercent, 2)}%` : '—'} tone={latest && latest.errorRatePercent >= 5 ? 'danger' : undefined} />
        <MetricTile label="P95 latency" value={latest ? `${formatNumber(latest.p95LatencyMs)} ms` : '—'} tone={latest && latest.p95LatencyMs >= 1_000 ? 'danger' : undefined} />
      </div>

      <section className="mt-4 rounded-lg border border-border p-3">
        <div className="mb-3 flex items-center justify-between">
          <h3 className="text-[11px] font-semibold text-text-strong">Request rate</h3>
          <span className="text-[9px] text-muted">Latest 48 samples</span>
        </div>
        <div className="h-40">
          {chartData.length === 0 ? (
            <div className="grid h-full place-items-center text-[10px] text-muted">Waiting for telemetry</div>
          ) : (
            <ResponsiveContainer width="100%" height="100%">
              <AreaChart data={chartData} margin={{ top: 5, right: 4, bottom: 0, left: -28 }}>
                <defs>
                  <linearGradient id="service-request-gradient" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="0%" stopColor="var(--chart-blue)" stopOpacity={0.32} />
                    <stop offset="100%" stopColor="var(--chart-blue)" stopOpacity={0} />
                  </linearGradient>
                </defs>
                <CartesianGrid stroke="var(--chart-grid)" vertical={false} />
                <XAxis dataKey="label" axisLine={false} tickLine={false} tick={{ fill: 'var(--muted)', fontSize: 8 }} minTickGap={34} />
                <YAxis axisLine={false} tickLine={false} tick={{ fill: 'var(--muted)', fontSize: 8 }} />
                <Tooltip contentStyle={{ border: '1px solid var(--border)', borderRadius: 8, background: 'var(--surface-elevated)', color: 'var(--text-strong)', fontSize: 10 }} />
                <Area type="monotone" dataKey="requests" stroke="var(--chart-blue)" strokeWidth={2} fill="url(#service-request-gradient)" isAnimationActive={false} />
              </AreaChart>
            </ResponsiveContainer>
          )}
        </div>
      </section>

      <section className="mt-4">
        <h3 className="mb-2 text-[11px] font-semibold text-text-strong">Current resources</h3>
        <div className="grid grid-cols-2 gap-2">
          <ResourceTile icon={<Cpu size={14} />} label="CPU" value={latest ? `${formatNumber(latest.cpuUsagePercent, 1)}%` : '—'} />
          <ResourceTile icon={<MemoryStick size={14} />} label="Memory" value={latest ? `${formatNumber(latest.memoryUsagePercent, 1)}%` : '—'} />
          <ResourceTile icon={<Database size={14} />} label="DB connections" value={latest ? formatNumber(latest.activeDbConnections) : '—'} />
          <ResourceTile icon={<Gauge size={14} />} label="Queue depth" value={latest ? formatNumber(latest.queueDepth) : '—'} />
        </div>
      </section>

      {latest && (
        <p className="mt-4 flex items-center gap-1.5 text-[9px] text-muted">
          <Activity size={12} /> Last sample {new Date(latest.timestampUtc).toLocaleString()}
        </p>
      )}
    </DetailPanel>
  )
}

function MetricTile({ label, value, tone }: { label: string; value: string; tone?: 'danger' }) {
  return (
    <div className="min-w-0 rounded-lg border border-border bg-surface-muted/45 p-2.5">
      <span className="block truncate text-[8px] text-muted">{label}</span>
      <strong className={`mt-1 block truncate text-sm ${tone === 'danger' ? 'text-danger' : 'text-text-strong'}`}>{value}</strong>
    </div>
  )
}

function ResourceTile({ icon, label, value }: { icon: ReactNode; label: string; value: string }) {
  return (
    <div className="flex items-center gap-2 rounded-lg border border-border-subtle bg-surface-muted/40 p-2.5">
      <span className="text-primary">{icon}</span>
      <div className="min-w-0">
        <span className="block truncate text-[8px] text-muted">{label}</span>
        <strong className="block truncate text-[11px] text-text-strong">{value}</strong>
      </div>
    </div>
  )
}
