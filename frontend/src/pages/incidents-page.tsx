import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Badge } from '../components/ui/badge'
import { Button } from '../components/ui/button'
import { Card } from '../components/ui/card'
import { incidentsApi } from '../services/incidents-api'
import type { Incident } from '../types/api'
import { formatTime } from '../utils/formatters'
import { EmptyState, PageFrame } from './services-page'

export function IncidentsPage() {
  const queryClient = useQueryClient()
  const query = useQuery({
    queryKey: ['incidents'],
    queryFn: ({ signal }) => incidentsApi.get(100, signal),
  })
  const mutation = useMutation({
    mutationFn: ({ id, status }: { id: number; status: Incident['status'] }) =>
      incidentsApi.updateStatus(id, status),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['incidents'] })
      void queryClient.invalidateQueries({ queryKey: ['recent-incidents'] })
    },
  })
  const incidents = query.data ?? []

  return (
    <PageFrame title="Incidents" description="Review and update incidents created through approval-gated actions.">
      {incidents.length === 0 && !query.isLoading ? <EmptyState label="No incidents have been created yet." /> : (
        <div className="space-y-3">
          {incidents.map((incident) => (
            <Card className="flex flex-wrap items-center gap-4 px-4 py-3" key={incident.id}>
              <div className="min-w-0 flex-1">
                <div className="flex flex-wrap items-center gap-2">
                  <strong className="text-sm text-text-strong">#{incident.id} {incident.title}</strong>
                  <Badge>{incident.severity}</Badge>
                  <Badge className={incident.status === 'Resolved' ? 'text-success' : incident.status === 'Investigating' ? 'text-warning' : 'text-danger'}>{incident.status}</Badge>
                </div>
                {incident.description && <p className="mt-1 line-clamp-2 text-xs text-muted">{incident.description}</p>}
                <time className="mt-1.5 block text-[10px] text-muted" dateTime={incident.startedAtUtc}>Started {formatTime(incident.startedAtUtc)}</time>
              </div>
              <div className="flex gap-2">
                {incident.status === 'Open' && (
                  <Button size="sm" variant="outline" disabled={mutation.isPending} onClick={() => mutation.mutate({ id: incident.id, status: 'Investigating' })}>Investigate</Button>
                )}
                {incident.status !== 'Resolved' && (
                  <Button size="sm" disabled={mutation.isPending} onClick={() => mutation.mutate({ id: incident.id, status: 'Resolved' })}>Resolve</Button>
                )}
              </div>
            </Card>
          ))}
        </div>
      )}
    </PageFrame>
  )
}
