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

interface LogEventResponse {
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

async function getJson<T>(path: string, signal?: AbortSignal): Promise<T> {
  const response = await fetch(path, { signal })

  if (!response.ok) {
    throw new Error(`API request failed with status ${response.status}`)
  }

  return response.json() as Promise<T>
}

export function fetchServices({ signal }: { signal: AbortSignal }) {
  return getJson<Service[]>('/api/services', signal)
}

export function fetchSimulationScenario({ signal }: { signal: AbortSignal }) {
  return getJson<SimulationScenarioResponse>('/api/simulation/scenario', signal)
}

export async function setSimulationScenario(scenario: SimulationScenario) {
  const response = await fetch('/api/simulation/scenario', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ scenario }),
  })

  if (!response.ok) {
    throw new Error(`API request failed with status ${response.status}`)
  }

  return response.json() as Promise<SimulationScenarioResponse>
}

export async function fetchLatestMetrics(
  serviceIds: number[],
  signal: AbortSignal,
) {
  const entries = await Promise.all(
    serviceIds.map(async (serviceId) => {
      const metrics = await getJson<ServiceMetric[]>(
        `/api/services/${serviceId}/metrics`,
        signal,
      )

      return [serviceId, metrics[0] ?? null] as const
    }),
  )

  return Object.fromEntries(entries) as Record<number, ServiceMetric | null>
}

export async function fetchRecentLogs(
  services: Service[],
  signal: AbortSignal,
) {
  const logsByService = await Promise.all(
    services.map(async (service) => {
      const logs = await getJson<LogEventResponse[]>(
        `/api/services/${service.id}/logs`,
        signal,
      )

      return logs.map((log) => ({
        ...log,
        serviceId: service.id,
        serviceName: service.name,
      }))
    }),
  )

  return logsByService
    .flat()
    .sort((left, right) =>
      Date.parse(right.timestampUtc) - Date.parse(left.timestampUtc))
    .slice(0, 20)
}
