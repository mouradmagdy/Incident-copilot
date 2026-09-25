import { Activity, Clock3, Server, ShieldAlert } from 'lucide-react'
import type { DashboardSummary } from '../../types/dashboard'
import { formatNumber } from '../../utils/formatters'
import { MetricCard } from './metric-card'

export function SummaryGrid({ serviceCount, summary }: { serviceCount: number; summary: DashboardSummary }) {
  return (
    <section className="summary-grid" aria-label="System summary">
      <MetricCard
        label="Services"
        value={formatNumber(serviceCount)}
        icon={<Server size={18} />}
        hint={<><i className="hint-dot hint-dot--healthy" />{serviceCount - summary.degradedCount} healthy <i className="hint-dot hint-dot--warning" />{summary.degradedCount} degraded</>}
      />
      <MetricCard label="Request Rate" value={`${formatNumber(summary.requestRate)} /s`} icon={<Activity size={18} />} hint="Across monitored services" />
      <MetricCard label="Error Rate" value={`${formatNumber(summary.errorRate, 1)}%`} icon={<ShieldAlert size={18} />} hint="Average latest sample" tone="red" />
      <MetricCard label="P95 Latency" value={`${formatNumber(summary.p95Latency)} ms`} icon={<Clock3 size={18} />} hint="Highest current service" />
    </section>
  )
}
