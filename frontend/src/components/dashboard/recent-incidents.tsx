import { ShieldAlert } from 'lucide-react'
import { Button } from '../ui/button'
import { Card } from '../ui/card'
import { CardContent } from '../ui/card-content'
import { CardHeader } from '../ui/card-header'

export function RecentIncidents() {
  return (
    <Card>
      <CardHeader>
        <strong>Recent Incidents</strong>
        <Button variant="outline" size="sm" type="button">View all</Button>
      </CardHeader>
      <CardContent className="flex min-h-[142px] items-center gap-[11px]">
        <span className="grid size-[34px] shrink-0 place-items-center rounded-full bg-surface-muted text-muted"><ShieldAlert size={18} /></span>
        <div>
          <strong className="block text-[10px] text-text-strong">No incident records yet</strong>
          <p className="mt-1 text-[9px] leading-[1.4] text-muted">Incident tracking will appear here when its backend is connected.</p>
        </div>
      </CardContent>
    </Card>
  )
}
