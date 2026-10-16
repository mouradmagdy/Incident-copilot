import { ApiError } from './base-api-service'
import type { PendingAction } from '../types/api'

export type PendingActionDecision = 'confirm' | 'cancel'

export const pendingActionsApi = {
  async decide(
    id: string,
    decision: PendingActionDecision,
  ): Promise<PendingAction> {
    const response = await fetch(
      `/api/pending-actions/${encodeURIComponent(id)}/${decision}`,
      {
        method: 'POST',
        headers: {
          Accept: 'application/json',
        },
      },
    )

    if (!response.ok) {
      throw new ApiError(
        response.status,
        `Pending action request failed with status ${response.status}`,
      )
    }

    return response.json() as Promise<PendingAction>
  },
}
