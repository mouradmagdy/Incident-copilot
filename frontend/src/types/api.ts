export interface AuthUser {
  id: string
  displayName: string
  email: string
}

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

export interface LogEventPage {
  items: LogEvent[]
  totalCount: number
  page: number
  pageSize: number
}

export interface LogVolumeBucket {
  bucketStartUtc: string
  infoCount: number
  warningCount: number
  errorCount: number
}

export interface Incident {
  id: number
  title: string
  description: string | null
  severity: 'Low' | 'Medium' | 'High' | 'Critical'
  status: 'Open' | 'Investigating' | 'Resolved'
  startedAtUtc: string
  resolvedAtUtc: string | null
  rootCause: string | null
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

export interface ChatToolCallDebug {
  name: string
  durationMs: number | null
  success: boolean | null
}

export interface ChatDebugInfo {
  sources: ChatSource[]
  toolCalls: ChatToolCallDebug[]
  llmRoundCount: number
  totalLatencyMs: number
  pendingActionState: string
}

export interface ConversationSummary {
  id: string
  title: string
  createdAtUtc: string
  updatedAtUtc: string
}

export interface ConversationMessage {
  id: number
  role: 'user' | 'assistant'
  content: string
  createdAtUtc: string
  sources: ChatSource[]
  debug: ChatDebugInfo | null
  pendingAction: PendingAction | null
}

export interface Conversation extends ConversationSummary {
  messages: ConversationMessage[]
}

export type PendingActionStatus =
  | 'Pending'
  | 'Completed'
  | 'Cancelled'
  | 'Expired'
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
  assistantResponse: string | null
}
