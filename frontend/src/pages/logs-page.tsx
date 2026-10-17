import { useQuery } from '@tanstack/react-query'
import { ChevronDown, ChevronLeft, ChevronRight, MoreHorizontal, Search } from 'lucide-react'
import { useEffect, useState, type ReactNode } from 'react'
import { LogDetailsPanel, LogLevelBadge } from '../components/logs/log-details-panel'
import { LogVolumeChart } from '../components/logs/log-volume-chart'
import { Button } from '../components/ui/button'
import { Card } from '../components/ui/card'
import { logsApi } from '../services/logs-api'
import { servicesApi } from '../services/services-api'
import type { TimeRange } from '../stores/ui-store'
import type { LogEvent } from '../types/api'
import { formatTime, getLogLevel } from '../utils/formatters'
import { PageFrame } from './services-page'

const levels = ['All', 'Debug', 'Information', 'Warning', 'Error', 'Critical']
const pageSize = 20

export function LogsPage() {
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [serviceId, setServiceId] = useState('All')
  const [level, setLevel] = useState('All')
  const [range, setRange] = useState<TimeRange>('24h')
  const [page, setPage] = useState(1)
  const [selectedLog, setSelectedLog] = useState<LogEvent | null>(null)

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setSearch(searchInput.trim())
      setPage(1)
    }, 300)
    return () => window.clearTimeout(timeout)
  }, [searchInput])

  const servicesQuery = useQuery({
    queryKey: ['services'],
    queryFn: ({ signal }) => servicesApi.getServices(signal),
    staleTime: 60_000,
  })
  const logsQuery = useQuery({
    queryKey: ['logs', { serviceId, level, range, search, page, pageSize }],
    queryFn: ({ signal }) => logsApi.getPage({
      serviceIds: serviceId === 'All' ? [] : [Number(serviceId)],
      level: level === 'All' ? undefined : level,
      range,
      search,
      page,
      pageSize,
    }, signal),
    refetchInterval: 15_000,
    placeholderData: (previousData) => previousData,
  })
  const volumeQuery = useQuery({
    queryKey: ['log-volume', { serviceId, level, range, search }],
    queryFn: ({ signal }) => logsApi.getVolume({
      serviceIds: serviceId === 'All' ? [] : [Number(serviceId)],
      level: level === 'All' ? undefined : level,
      range,
      search,
    }, signal),
    refetchInterval: 15_000,
  })
  const logs = logsQuery.data?.items ?? []
  const totalCount = logsQuery.data?.totalCount ?? 0
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
  const firstResult = totalCount === 0 ? 0 : (page - 1) * pageSize + 1
  const lastResult = Math.min(page * pageSize, totalCount)

  function resetFilters() {
    setSearchInput('')
    setSearch('')
    setServiceId('All')
    setLevel('All')
    setPage(1)
  }

  return (
    <PageFrame title="Logs" description="Search and analyze log events across all monitored services." fitViewport>
      <div className="logs-page h-full min-h-0">
        <div className="flex h-full min-h-0 min-w-0 flex-col gap-3">
          <Card className="flex shrink-0 flex-wrap items-center gap-2 p-3">
              <label className="relative min-w-[190px] flex-1">
                <span className="sr-only">Search logs</span>
                <Search className="pointer-events-none absolute top-1/2 left-3 -translate-y-1/2 text-muted" size={14} />
                <input
                  className="h-9 w-full rounded-[7px] border border-border bg-surface pl-9 pr-3 text-[11px] text-text-strong outline-none placeholder:text-muted focus-visible:border-primary"
                  placeholder="Search message, exception, or trace ID..."
                  value={searchInput}
                  onChange={(event) => setSearchInput(event.target.value)}
                />
              </label>
              <FilterSelect label="Service" value={serviceId} onChange={(value) => { setServiceId(value); setPage(1) }}>
                <option value="All">All services</option>
                {(servicesQuery.data ?? []).map((service) => <option key={service.id} value={service.id}>{service.name}</option>)}
              </FilterSelect>
              <FilterSelect label="Level" value={level} onChange={(value) => { setLevel(value); setPage(1) }}>
                {levels.map((value) => <option key={value} value={value}>{value === 'All' ? 'All levels' : value}</option>)}
              </FilterSelect>
              <FilterSelect label="Time range" value={range} onChange={(value) => { setRange(value as TimeRange); setPage(1) }}>
                <option value="1h">Last 1h</option>
                <option value="6h">Last 6h</option>
                <option value="24h">Last 24h</option>
                <option value="7d">Last 7d</option>
              </FilterSelect>
              {(searchInput || serviceId !== 'All' || level !== 'All') && <Button variant="outline" type="button" onClick={resetFilters}>Reset</Button>}
          </Card>

          <div className="shrink-0">
            <LogVolumeChart
              buckets={volumeQuery.data ?? []}
              range={range}
              totalCount={totalCount}
              isLoading={volumeQuery.isPending}
            />
          </div>

          <Card className="flex min-h-0 flex-1 flex-col overflow-hidden">
            {logs.length === 0 && !logsQuery.isLoading ? (
              <div className="grid min-h-0 flex-1 place-items-center text-[11px] text-muted">No log events match these filters.</div>
            ) : (
                <div className="min-h-0 max-w-full flex-1 overflow-auto">
                  <table className="w-full min-w-[700px] border-collapse text-left text-[10px]">
                    <thead className="sticky top-0 z-10 bg-surface-muted text-[9px] text-muted shadow-[0_1px_0_var(--border)]">
                      <tr>
                        <th className="px-3 py-2.5 font-medium">Time</th>
                        <th className="px-3 py-2.5 font-medium">Level</th>
                        <th className="px-3 py-2.5 font-medium">Service</th>
                        <th className="px-3 py-2.5 font-medium">Message</th>
                        <th className="px-3 py-2.5 font-medium">Trace ID</th>
                        <th className="px-3 py-2.5"><span className="sr-only">Details</span></th>
                      </tr>
                    </thead>
                    <tbody>
                      {logs.map((log) => {
                        const logLevel = getLogLevel(log.level)
                        return (
                          <tr
                            className={`cursor-pointer border-t border-border-subtle transition-colors hover:bg-surface-muted/55 ${selectedLog?.id === log.id ? 'bg-primary-soft/50' : ''}`}
                            key={log.id}
                            onClick={() => setSelectedLog(log)}
                          >
                            <td className="whitespace-nowrap px-3 py-2.5 text-muted"><time dateTime={log.timestampUtc}>{formatTime(log.timestampUtc)}</time></td>
                            <td className="px-3 py-2.5"><LogLevelBadge level={logLevel} /></td>
                            <td className="max-w-36 truncate px-3 py-2.5 font-medium text-text-strong">{log.serviceName}</td>
                            <td className={`max-w-[360px] truncate px-3 py-2.5 ${logLevel === 'Error' || logLevel === 'Critical' ? 'text-danger' : logLevel === 'Warning' ? 'text-warning' : 'text-text'}`}>{log.message}</td>
                            <td className="max-w-28 truncate px-3 py-2.5 font-mono text-[8px] text-muted">{log.traceId ?? '—'}</td>
                            <td className="px-3 py-2.5 text-right"><Button variant="ghost" size="icon" type="button" aria-label={`View log ${log.id} details`} onClick={() => setSelectedLog(log)}><MoreHorizontal size={14} /></Button></td>
                          </tr>
                        )
                      })}
                    </tbody>
                  </table>
                </div>
            )}
            <footer className="flex min-h-10 shrink-0 items-center justify-between gap-3 border-t border-border px-3 py-2 text-[9px] text-muted">
              <span>Showing {firstResult}-{lastResult} of {totalCount}</span>
              <div className="flex items-center gap-2">
                <span>Page {page} of {totalPages}</span>
                <Button variant="outline" size="icon" type="button" className="size-7" aria-label="Previous page" disabled={page <= 1 || logsQuery.isFetching} onClick={() => setPage((current) => Math.max(1, current - 1))}>
                  <ChevronLeft size={13} />
                </Button>
                <Button variant="outline" size="icon" type="button" className="size-7" aria-label="Next page" disabled={page >= totalPages || logsQuery.isFetching} onClick={() => setPage((current) => Math.min(totalPages, current + 1))}>
                  <ChevronRight size={13} />
                </Button>
              </div>
            </footer>
          </Card>

          {selectedLog && (
            <LogDetailsPanel log={selectedLog} onClose={() => setSelectedLog(null)} />
          )}
        </div>
      </div>
    </PageFrame>
  )
}

function FilterSelect({ label, value, onChange, children }: { label: string; value: string; onChange: (value: string) => void; children: ReactNode }) {
  return (
    <label className="relative">
      <span className="sr-only">{label}</span>
      <select className="h-9 min-w-28 cursor-pointer appearance-none rounded-[7px] border border-border bg-surface py-0 pr-8 pl-3 text-[10px] text-text outline-none focus-visible:border-primary" value={value} onChange={(event) => onChange(event.target.value)}>
        {children}
      </select>
      <ChevronDown className="pointer-events-none absolute top-1/2 right-2.5 -translate-y-1/2 text-muted" size={12} />
    </label>
  )
}
