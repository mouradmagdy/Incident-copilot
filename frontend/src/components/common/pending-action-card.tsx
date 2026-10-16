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
  Failed: 'bg-danger-soft text-danger',
}

export function PendingActionCard({
  action,
  isUpdating,
  hasError,
  onDecision,
}: PendingActionCardProps) {
  const isPending = action.status === 'Pending'

  return (
    <Card className="mt-2 overflow-hidden border-warning/35 bg-warning-soft/30">
      <CardHeader className="gap-2 border-b border-warning/20">
        <span className="flex min-w-0 items-center gap-2 font-semibold">
          <span className="grid size-6 shrink-0 place-items-center rounded-md bg-warning-soft text-warning">
            <ShieldAlert size={14} />
          </span>
          Approval required
        </span>
        <span
          className={`shrink-0 rounded-full px-2 py-0.5 text-[8px] font-semibold ${statusStyles[action.status]}`}
        >
          {action.status}
        </span>
      </CardHeader>

      <CardContent className="pt-3">
        <p className="m-0 text-[10px] font-medium leading-relaxed text-text-strong">
          {action.description}
        </p>
        <p className="mt-1.5 mb-0 text-[9px] leading-relaxed text-muted">
          This changes system state and will not run without confirmation.
        </p>

        {action.status === 'Completed' && (
          <p className="mt-2 mb-0 flex items-center gap-1.5 text-[9px] font-medium text-success">
            <CheckCircle2 size={12} />
            Action completed successfully.
          </p>
        )}

        {action.status === 'Cancelled' && (
          <p className="mt-2 mb-0 flex items-center gap-1.5 text-[9px] font-medium text-muted">
            <XCircle size={12} />
            Action cancelled. Nothing was executed.
          </p>
        )}

        {action.status === 'Failed' && (
          <p className="mt-2 mb-0 text-[9px] leading-relaxed text-danger" role="alert">
            {action.error ?? 'The action could not be completed.'}
          </p>
        )}

        {hasError && (
          <p className="mt-2 mb-0 text-[9px] text-danger" role="alert">
            The request failed. The action was not changed; please try again.
          </p>
        )}

        {isPending && (
          <div className="mt-3 grid grid-cols-2 gap-2">
            <Button
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
