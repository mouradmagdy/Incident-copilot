import { Activity, Clock3, Server, ShieldAlert } from 'lucide-react'
import type { DashboardSummary } from '../../types/dashboard'
import { formatNumber } from '../../utils/formatters'
import { MetricCard } from './metric-card'

export function SummaryGrid({ serviceCount, summary }: { serviceCount: number; summary: DashboardSummary }) {
  return (
    <section className="grid grid-cols-4 gap-2.5 max-[940px]:grid-cols-2 max-[500px]:grid-cols-1" aria-label="System summary">
      <MetricCard
        label="Services"
        value={formatNumber(serviceCount)}
        icon={<Server size={18} />}
        hint={<><i className="size-[7px] rounded-full bg-success first:ml-0" />{serviceCount - summary.degradedCount} healthy <i className="ml-[3px] size-[7px] rounded-full bg-warning first:ml-0" />{summary.degradedCount} degraded</>}
      />
      <MetricCard label="Request Rate" value={`${formatNumber(summary.requestRate)} /s`} icon={<Activity size={18} />} hint="Across monitored services" />
      <MetricCard label="Error Rate" value={`${formatNumber(summary.errorRate, 1)}%`} icon={<ShieldAlert size={18} />} hint="Average latest sample" tone="red" />
      <MetricCard label="P95 Latency" value={`${formatNumber(summary.p95Latency)} ms`} icon={<Clock3 size={18} />} hint="Highest current service" />
    </section>
  )
}
