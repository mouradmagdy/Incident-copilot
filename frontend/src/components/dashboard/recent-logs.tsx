import type { LogEvent } from '../../types/api'
import { formatTime, getLogLevel } from '../../utils/formatters'
import { Badge } from '../ui/badge'
import { Card, CardContent, CardHeader } from '../ui/card'

export function RecentLogs({ logs, isLoading }: { logs: LogEvent[]; isLoading: boolean }) {
  return (
    <Card className="logs-card">
      <CardHeader>
        <strong>Recent Logs</strong>
        <span className="panel-meta">{logs.length} events</span>
      </CardHeader>
      <CardContent className="logs-list">
        {isLoading && <div className="panel-empty">Loading recent logs…</div>}
        {!isLoading && logs.length === 0 && <div className="panel-empty">No recent log events</div>}
        {logs.slice(0, 8).map((log) => {
          const level = getLogLevel(log.level)
          return (
            <div className="log-row" key={`${log.serviceId}-${log.id}`}>
              <Badge className={`log-badge log-badge--${level.toLowerCase()}`}>{level}</Badge>
              <div className="log-copy">
                <strong>{log.serviceName}</strong>
                <span>{log.message}</span>
              </div>
              <time>{formatTime(log.timestampUtc)}</time>
            </div>
          )
        })}
      </CardContent>
    </Card>
  )
}
