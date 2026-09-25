import { ShieldAlert } from 'lucide-react'
import { Button } from '../ui/button'
import { Card, CardContent, CardHeader } from '../ui/card'

export function RecentIncidents() {
  return (
    <Card className="incidents-card">
      <CardHeader>
        <strong>Recent Incidents</strong>
        <Button variant="outline" size="sm" type="button">View all</Button>
      </CardHeader>
      <CardContent className="incidents-empty">
        <span className="empty-icon"><ShieldAlert size={18} /></span>
        <div>
          <strong>No incident records yet</strong>
          <p>Incident tracking will appear here when its backend is connected.</p>
        </div>
      </CardContent>
    </Card>
  )
}
