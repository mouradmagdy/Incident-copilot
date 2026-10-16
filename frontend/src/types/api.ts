export interface Service {
  id: number
  name: string
  description: string
  isActive: boolean
}

export interface ServiceMetric {
  id: number
  timestampUtc: string
  requestsPerSecond: number
  averageLatencyMs: number
  p95LatencyMs: number
  errorRatePercent: number
  cpuUsagePercent: number
  memoryUsagePercent: number
  queueDepth: number
  activeDbConnections: number
}

export interface LogEventResponse {
  id: number
  timestampUtc: string
  level: number | string
  message: string
  exceptionType: string | null
  traceId: string | null
}

export interface LogEvent extends LogEventResponse {
  serviceId: number
  serviceName: string
}

export type SimulationScenario = 'Healthy' | 'DatabaseConnectionExhaustion'

export interface SimulationScenarioResponse {
  scenario: SimulationScenario
}

export interface ChatResponse {
  content: string
  sources: ChatSource[]
}

export interface ChatSource {
  chunkId: number
  fileName: string
  section: string | null
}

export type PendingActionStatus =
  | 'Pending'
  | 'Completed'
  | 'Cancelled'
  | 'Failed'

export interface PendingAction {
  id: string
  toolName: string
  description: string
  status: PendingActionStatus
  createdAtUtc: string
  decidedAtUtc: string | null
  resultJson: string | null
  error: string | null
}
