import { ShieldAlert } from 'lucide-react'
import { Link } from 'react-router-dom'
import type { Incident } from '../../types/api'
import { formatTime } from '../../utils/formatters'
import { Badge } from '../ui/badge'
import { Button } from '../ui/button'
import { Card } from '../ui/card'
import { CardContent } from '../ui/card-content'
import { CardHeader } from '../ui/card-header'

export function RecentIncidents({ incidents, isLoading }: { incidents: Incident[]; isLoading: boolean }) {
  return (
    <Card className="flex h-full flex-col">
      <CardHeader>
        <strong>Recent Incidents</strong>
        <Button asChild variant="outline" size="sm"><Link to="/incidents">View all</Link></Button>
      </CardHeader>
      <CardContent className="min-h-[112px] flex-1">
        {isLoading ? (
          <div className="grid h-full place-items-center text-[10px] text-muted">Loading incidents…</div>
        ) : incidents.length === 0 ? (
          <div className="flex h-full items-center gap-[11px]">
            <span className="grid size-[34px] shrink-0 place-items-center rounded-full bg-surface-muted text-muted"><ShieldAlert size={18} /></span>
            <div>
              <strong className="block text-[10px] text-text-strong">No incident records yet</strong>
              <p className="mt-1 text-[9px] leading-[1.4] text-muted">Confirmed incident actions will appear here.</p>
            </div>
          </div>
        ) : (
          <div className="divide-y divide-border-subtle">
            {incidents.slice(0, 3).map((incident) => (
              <article className="flex min-w-0 items-center gap-2 py-2 first:pt-0 last:pb-0" key={incident.id}>
                <span className={`size-2 shrink-0 rounded-full ${incident.status === 'Resolved' ? 'bg-success' : 'bg-danger'}`} />
                <div className="min-w-0 flex-1">
                  <strong className="block truncate text-[10px] text-text-strong">#{incident.id} {incident.title}</strong>
                  <span className="mt-0.5 block text-[8px] text-muted">{formatTime(incident.startedAtUtc)}</span>
                </div>
                <Badge className="shrink-0">{incident.severity}</Badge>
              </article>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  )
}
