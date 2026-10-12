import type { LogEvent } from '../../types/api'
import { cn } from '../../utils/classnames'
import { formatTime, getLogLevel } from '../../utils/formatters'
import { Badge } from '../ui/badge'
import { Card } from '../ui/card'
import { CardContent } from '../ui/card-content'
import { CardHeader } from '../ui/card-header'

export function RecentLogs({ logs, isLoading }: { logs: LogEvent[]; isLoading: boolean }) {
  return (
    <Card className="mt-2.5">
      <CardHeader>
        <strong>Recent Logs</strong>
        <span className="text-[9px] text-muted">{logs.length} events</span>
      </CardHeader>
      <CardContent className="grid grid-cols-2 gap-x-4 max-[940px]:grid-cols-1">
        {isLoading && <div className="col-span-full grid min-h-[76px] place-items-center text-center text-[10px] text-muted">Loading recent logs…</div>}
        {!isLoading && logs.length === 0 && <div className="col-span-full grid min-h-[76px] place-items-center text-center text-[10px] text-muted">No recent log events</div>}
        {logs.slice(0, 8).map((log) => {
          const level = getLogLevel(log.level)
          return (
            <div className="grid min-w-0 grid-cols-[56px_minmax(0,1fr)_auto] items-center gap-2 border-t border-border-subtle py-[9px]" key={`${log.serviceId}-${log.id}`}>
              <Badge className={cn(
                'justify-center bg-surface-muted px-1.5 py-[3px] text-text',
                level === 'Warning' && 'bg-warning-soft text-warning',
                (level === 'Error' || level === 'Critical') && 'bg-danger-soft text-danger',
              )}>{level}</Badge>
              <div className="min-w-0">
                <strong className="mb-0.5 block truncate text-[9px] text-text-strong">{log.serviceName}</strong>
                <span className="block truncate text-[8px] text-muted">{log.message}</span>
              </div>
              <time className="text-[8px] text-muted">{formatTime(log.timestampUtc)}</time>
            </div>
          )
        })}
      </CardContent>
    </Card>
  )
}
