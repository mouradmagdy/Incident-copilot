import {
  Check,
  CheckCircle2,
  LoaderCircle,
  ShieldAlert,
  X,
  XCircle,
} from 'lucide-react'
import type { PendingAction } from '../../types/api'
import type { PendingActionDecision } from '../../services/pending-actions-api'
import { Button } from '../ui/button'
import { Card } from '../ui/card'
import { CardContent } from '../ui/card-content'
import { CardHeader } from '../ui/card-header'
import { parseCreatedIncidentResult } from '../../utils/pending-action'

interface PendingActionCardProps {
  action: PendingAction
  isUpdating: boolean
  hasError: boolean
  onDecision: (id: string, decision: PendingActionDecision) => void
}

const statusStyles: Record<PendingAction['status'], string> = {
  Pending: 'bg-warning-soft text-warning',
  Completed: 'bg-success-soft text-success',
  Cancelled: 'bg-surface-muted text-muted',
  Expired: 'bg-surface-muted text-muted',
  Failed: 'bg-danger-soft text-danger',
}

const cardStyles: Record<PendingAction['status'], string> = {
  Pending: 'border-warning/35 bg-warning-soft/30',
  Completed: 'border-success/35 bg-success-soft/25',
  Cancelled: 'border-border bg-surface-muted/55',
  Expired: 'border-border bg-surface-muted/55',
  Failed: 'border-danger/35 bg-danger-soft/25',
}

const headerLabels: Record<PendingAction['status'], string> = {
  Pending: 'Approval required',
  Completed: 'Incident created',
  Cancelled: 'Action cancelled',
  Expired: 'Approval expired',
  Failed: 'Action failed',
}

export function PendingActionCard({
  action,
  isUpdating,
  hasError,
  onDecision,
}: PendingActionCardProps) {
  const isPending = action.status === 'Pending'
  const incident = parseCreatedIncidentResult(action)

  return (
    <Card className={`mt-2 overflow-hidden ${cardStyles[action.status]}`}>
      <CardHeader className="items-start gap-2 border-b border-border/65 text-xs">
        <span className="flex min-w-0 items-center gap-2 font-semibold">
          <span
            className={`grid size-6 shrink-0 place-items-center rounded-md ${statusStyles[action.status]}`}
          >
            {action.status === 'Completed' ? (
              <CheckCircle2 size={14} />
            ) : action.status === 'Pending' ? (
              <ShieldAlert size={14} />
            ) : (
              <XCircle size={14} />
            )}
          </span>
          {headerLabels[action.status]}
        </span>
        <span
          className={`shrink-0 rounded-full px-2 py-0.5 text-[10px] font-semibold ${statusStyles[action.status]}`}
        >
          {action.status}
        </span>
      </CardHeader>

      <CardContent className="pt-3">
        <p className="m-0 text-xs font-medium leading-relaxed text-text-strong">
          {action.description}
        </p>
        {isPending && (
          <p className="mt-1.5 mb-0 text-[11px] leading-relaxed text-muted">
            This changes system state and will not run without confirmation.
          </p>
        )}

        {action.status === 'Completed' && (
          <div className="mt-2 rounded-md border border-success/20 bg-surface/65 p-2.5">
            {incident ? (
              <dl className="m-0 grid grid-cols-[auto_minmax(0,1fr)] gap-x-2 gap-y-1 text-[11px]">
                <dt className="text-muted">Incident</dt>
                <dd className="m-0 truncate font-semibold text-text-strong">
                  #{incident.id} — {incident.title}
                </dd>
                <dt className="text-muted">Severity</dt>
                <dd className="m-0 font-medium text-text-strong">
                  {incident.severity}
                </dd>
                <dt className="text-muted">Status</dt>
                <dd className="m-0 font-medium text-success">
                  {incident.status}
                </dd>
              </dl>
            ) : (
              <p className="m-0 flex items-center gap-1.5 text-[11px] font-medium text-success">
                <CheckCircle2 size={12} />
                Action completed successfully.
              </p>
            )}
          </div>
        )}

        {action.status === 'Cancelled' && (
          <p className="mt-2 mb-0 flex items-center gap-1.5 text-[11px] font-medium text-muted">
            <XCircle size={12} />
            Action cancelled. Nothing was executed.
          </p>
        )}

        {action.status === 'Expired' && (
          <p className="mt-2 mb-0 flex items-center gap-1.5 text-[11px] font-medium text-muted">
            <XCircle size={12} />
            This proposal expired without executing the action.
          </p>
        )}

        {action.status === 'Failed' && (
          <p className="mt-2 mb-0 text-[11px] leading-relaxed text-danger" role="alert">
            {action.error ?? 'The action could not be completed.'}
          </p>
        )}

        {hasError && (
          <p className="mt-2 mb-0 text-[11px] text-danger" role="alert">
            The request failed. The action was not changed; please try again.
          </p>
        )}

        {isPending && (
          <div className="mt-3 grid grid-cols-2 gap-2">
            <Button
              className="text-[11px]"
              type="button"
              size="sm"
              onClick={() => onDecision(action.id, 'confirm')}
              disabled={isUpdating}
            >
              {isUpdating ? (
                <LoaderCircle className="animate-spin" size={12} />
              ) : (
                <Check size={12} />
              )}
              Confirm
            </Button>
            <Button
              className="text-[11px]"
              type="button"
              variant="outline"
              size="sm"
              onClick={() => onDecision(action.id, 'cancel')}
              disabled={isUpdating}
            >
              <X size={12} />
              Cancel
            </Button>
          </div>
        )}
      </CardContent>
    </Card>
  )
}
