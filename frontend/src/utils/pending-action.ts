import type { PendingAction } from '../types/api'

export interface CreatedIncidentResult {
  id: number
  title: string
  severity: string
  status: string
  startedAtUtc: string
}

export function parseCreatedIncidentResult(
  action: PendingAction,
): CreatedIncidentResult | null {
  if (action.toolName !== 'create_incident' || !action.resultJson) {
    return null
  }

  try {
    const value = JSON.parse(action.resultJson) as Record<string, unknown>
    const id = value.id ?? value.Id
    const title = value.title ?? value.Title
    const severity = value.severity ?? value.Severity
    const status = value.status ?? value.Status
    const startedAtUtc = value.startedAtUtc ?? value.StartedAtUtc

    if (
      typeof id !== 'number' ||
      typeof title !== 'string' ||
      typeof severity !== 'string' ||
      typeof status !== 'string' ||
      typeof startedAtUtc !== 'string'
    ) {
      return null
    }

    return { id, title, severity, status, startedAtUtc }
  } catch {
    return null
  }
}

