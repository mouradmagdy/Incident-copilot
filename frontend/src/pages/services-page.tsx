import { useQuery } from '@tanstack/react-query'
import { Activity, AlertTriangle, ChevronDown, Search, Server, ShieldCheck, ShieldX } from 'lucide-react'
import { useState, type ReactNode } from 'react'
import { ServiceDetailsPanel } from '../components/services/service-details-panel'
import { Button } from '../components/ui/button'
import { Card } from '../components/ui/card'
import { CardContent } from '../components/ui/card-content'
import { CardHeader } from '../components/ui/card-header'
import { metricsApi } from '../services/metrics-api'
import { servicesApi } from '../services/services-api'
import type { TimeRange } from '../stores/ui-store'
import type { Service, ServiceMetric } from '../types/api'
import { formatNumber } from '../utils/formatters'
import { isMetricDegraded } from '../utils/telemetry'

type ServiceStatus = 'All' | 'Healthy' | 'Degraded' | 'Down'

export function ServicesPage() {
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState<ServiceStatus>('All')
  const [range, setRange] = useState<TimeRange>('24h')
  const [selectedServiceId, setSelectedServiceId] = useState<number | null>(null)
  const servicesQuery = useQuery({
    queryKey: ['services'],
    queryFn: ({ signal }) => servicesApi.getServices(signal),
    staleTime: 60_000,
  })
  const services = servicesQuery.data ?? []
  const serviceIds = services.map((service) => service.id)
  const metricsQuery = useQuery({
    queryKey: ['service-metrics', serviceIds, range],
    queryFn: ({ signal }) => metricsApi.get(serviceIds, range, signal),
    enabled: serviceIds.length > 0,
    refetchInterval: 15_000,
  })
  const metricsByService = metricsQuery.data ?? {}
  const serviceRows = services.map((service) => {
    const metrics = metricsByService[service.id] ?? []
    const latest = metrics[0]
    return { service, metrics, latest, status: getServiceStatus(service, latest) }
  })
  const filteredRows = serviceRows.filter((row) => {
    const matchesSearch = `${row.service.name} ${row.service.description}`.toLowerCase().includes(search.trim().toLowerCase())
    return matchesSearch && (status === 'All' || row.status === status)
  })
  const selectedRow = serviceRows.find((row) => row.service.id === selectedServiceId)

  return (
    <PageFrame title="Services" description="Monitor and understand the performance and health of all services.">
      <div className="services-page">
        <div className="min-w-0">
          <div className="min-w-0 space-y-3">
            <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
              <SummaryCard icon={<Server size={16} />} label="Total services" value={services.length} tone="primary" />
              <SummaryCard icon={<ShieldCheck size={16} />} label="Healthy" value={serviceRows.filter((row) => row.status === 'Healthy').length} tone="success" />
              <SummaryCard icon={<AlertTriangle size={16} />} label="Degraded" value={serviceRows.filter((row) => row.status === 'Degraded').length} tone="warning" />
              <SummaryCard icon={<ShieldX size={16} />} label="Down" value={serviceRows.filter((row) => row.status === 'Down').length} tone="danger" />
            </div>

            <Card className="overflow-hidden">
              <div className="flex flex-wrap items-center gap-2 border-b border-border p-3">
                <label className="relative min-w-[180px] flex-1">
                  <span className="sr-only">Search services</span>
                  <Search className="pointer-events-none absolute top-1/2 left-3 -translate-y-1/2 text-muted" size={14} />
                  <input
                    className="h-9 w-full rounded-[7px] border border-border bg-surface pl-9 pr-3 text-[11px] text-text-strong outline-none placeholder:text-muted focus-visible:border-primary"
                    placeholder="Search services..."
                    value={search}
                    onChange={(event) => setSearch(event.target.value)}
                  />
                </label>
                <SelectControl label="Status" value={status} onChange={(value) => setStatus(value as ServiceStatus)} options={['All', 'Healthy', 'Degraded', 'Down']} />
                <SelectControl label="Time range" value={range} onChange={(value) => setRange(value as TimeRange)} options={['1h', '6h', '24h', '7d']} />
                {(search || status !== 'All') && (
                  <Button variant="outline" type="button" onClick={() => { setSearch(''); setStatus('All') }}>Reset</Button>
                )}
              </div>

              <div className="max-w-full overflow-x-auto">
                <table className="w-full min-w-[760px] border-collapse text-left text-[10px]">
                  <thead className="bg-surface-muted/65 text-[9px] text-muted">
                    <tr>
                      <th className="px-3 py-2.5 font-medium">Service</th>
                      <th className="px-3 py-2.5 font-medium">Status</th>
                      <th className="px-3 py-2.5 font-medium">Requests / sec</th>
                      <th className="px-3 py-2.5 font-medium">Error rate</th>
                      <th className="px-3 py-2.5 font-medium">P95 latency</th>
                      <th className="px-3 py-2.5 font-medium">CPU</th>
                      <th className="px-3 py-2.5 font-medium">Trend</th>
                      <th className="px-3 py-2.5"><span className="sr-only">Actions</span></th>
                    </tr>
                  </thead>
                  <tbody>
                    {filteredRows.map(({ service, metrics, latest, status: rowStatus }) => (
                      <tr className={`border-t border-border-subtle transition-colors hover:bg-surface-muted/55 ${selectedServiceId === service.id ? 'bg-primary-soft/45' : ''}`} key={service.id}>
                        <td className="px-3 py-2.5">
                          <div className="flex items-center gap-2">
                            <span className="grid size-7 shrink-0 place-items-center rounded-md bg-primary-soft text-primary"><Server size={13} /></span>
                            <div className="min-w-0">
                              <strong className="block max-w-40 truncate text-text-strong">{service.name}</strong>
                              <span className="block max-w-40 truncate text-[8px] text-muted">{service.description}</span>
                            </div>
                          </div>
                        </td>
                        <td className="px-3 py-2.5"><ServiceStatusBadge status={rowStatus} /></td>
                        <td className="px-3 py-2.5 text-text">{latest ? formatNumber(latest.requestsPerSecond) : '—'}</td>
                        <td className={`px-3 py-2.5 ${latest && latest.errorRatePercent >= 5 ? 'font-semibold text-danger' : 'text-text'}`}>{latest ? `${formatNumber(latest.errorRatePercent, 2)}%` : '—'}</td>
                        <td className={`px-3 py-2.5 ${latest && latest.p95LatencyMs >= 1_000 ? 'font-semibold text-danger' : 'text-text'}`}>{latest ? `${formatNumber(latest.p95LatencyMs)} ms` : '—'}</td>
                        <td className="px-3 py-2.5 text-text">{latest ? `${formatNumber(latest.cpuUsagePercent, 1)}%` : '—'}</td>
                        <td className="px-3 py-2.5"><Sparkline metrics={metrics} degraded={rowStatus === 'Degraded'} /></td>
                        <td className="px-3 py-2.5 text-right"><Button variant="outline" size="sm" type="button" onClick={() => setSelectedServiceId(service.id)}>Details</Button></td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              {filteredRows.length === 0 && !servicesQuery.isLoading && (
                <div className="grid min-h-40 place-items-center text-[11px] text-muted">No services match these filters.</div>
              )}
            </Card>
          </div>

          {selectedRow && (
            <ServiceDetailsPanel service={selectedRow.service} metrics={selectedRow.metrics} onClose={() => setSelectedServiceId(null)} />
          )}
        </div>
      </div>
    </PageFrame>
  )
}

function SummaryCard({ icon, label, value, tone }: { icon: ReactNode; label: string; value: number; tone: 'primary' | 'success' | 'warning' | 'danger' }) {
  const tones = {
    primary: 'bg-primary-soft text-primary',
    success: 'bg-success-soft text-success',
    warning: 'bg-warning-soft text-warning',
    danger: 'bg-danger-soft text-danger',
  }
  return (
    <Card className="flex min-w-0 items-center gap-3 p-3">
      <span className={`grid size-9 shrink-0 place-items-center rounded-lg ${tones[tone]}`}>{icon}</span>
      <div className="min-w-0">
        <strong className="block text-lg leading-none text-text-strong">{value}</strong>
        <span className="mt-1 block truncate text-[9px] text-muted">{label}</span>
      </div>
    </Card>
  )
}

function SelectControl({ label, value, options, onChange }: { label: string; value: string; options: string[]; onChange: (value: string) => void }) {
  return (
    <label className="relative">
      <span className="sr-only">{label}</span>
      <select className="h-9 min-w-28 cursor-pointer appearance-none rounded-[7px] border border-border bg-surface py-0 pr-8 pl-3 text-[10px] text-text outline-none focus-visible:border-primary" value={value} onChange={(event) => onChange(event.target.value)}>
        {options.map((option) => <option key={option} value={option}>{option === '24h' ? 'Last 24h' : option === '7d' ? 'Last 7d' : option === '6h' ? 'Last 6h' : option === '1h' ? 'Last 1h' : option}</option>)}
      </select>
      <ChevronDown className="pointer-events-none absolute top-1/2 right-2.5 -translate-y-1/2 text-muted" size={12} />
    </label>
  )
}

function ServiceStatusBadge({ status }: { status: Exclude<ServiceStatus, 'All'> }) {
  const className = status === 'Healthy'
    ? 'bg-success-soft text-success'
    : status === 'Degraded'
      ? 'bg-warning-soft text-warning'
      : 'bg-danger-soft text-danger'
  return <span className={`inline-flex items-center gap-1 rounded-full px-2 py-1 text-[8px] font-semibold ${className}`}><span className="size-1.5 rounded-full bg-current" />{status}</span>
}

function Sparkline({ metrics, degraded }: { metrics: ServiceMetric[]; degraded: boolean }) {
  const values = metrics.slice(0, 18).reverse().map((metric) => metric.requestsPerSecond)
  if (values.length < 2) return <span className="text-muted">—</span>
  const min = Math.min(...values)
  const max = Math.max(...values)
  const span = Math.max(max - min, 1)
  const points = values.map((value, index) => `${(index / (values.length - 1)) * 52},${18 - ((value - min) / span) * 16}`).join(' ')
  return <svg className={degraded ? 'text-warning' : 'text-primary'} width="54" height="20" viewBox="0 0 54 20" role="img" aria-label="Request rate trend"><polyline points={points} fill="none" stroke="currentColor" strokeWidth="1.5" vectorEffect="non-scaling-stroke" /></svg>
}

function getServiceStatus(service: Service, metric: ServiceMetric | undefined): Exclude<ServiceStatus, 'All'> {
  if (!service.isActive) return 'Down'
  return isMetricDegraded(metric) ? 'Degraded' : 'Healthy'
}

export function PageFrame({ title, description, children, fitViewport = false }: { title: string; description: string; children: ReactNode; fitViewport?: boolean }) {
  return (
    <main className={fitViewport ? 'flex h-screen min-h-0 min-w-0 flex-col overflow-hidden px-5 py-4' : 'min-h-full min-w-0 px-5 py-5'}>
      <header className={fitViewport ? 'mb-3 shrink-0' : 'mb-5'}>
        <h1 className="text-2xl font-semibold tracking-[-.03em] text-text-strong">{title}</h1>
        <p className="mt-1 text-xs text-muted">{description}</p>
      </header>
      {fitViewport ? <div className="min-h-0 flex-1">{children}</div> : children}
    </main>
  )
}

export function EmptyState({ label }: { label: string }) {
  return (
    <Card>
      <CardHeader><strong>Nothing to show</strong></CardHeader>
      <CardContent className="grid min-h-40 place-items-center text-center text-xs text-muted">
        <span className="grid gap-2 place-items-center"><Activity size={22} />{label}</span>
      </CardContent>
    </Card>
  )
}
