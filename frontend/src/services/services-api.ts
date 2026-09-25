import { apiService } from './base-api-service'
import type { LogEvent, LogEventResponse, Service, ServiceMetric } from '../types/api'

export const servicesApi = {
  getServices(signal?: AbortSignal) {
    return apiService.get<Service[]>('/api/services', { signal })
  },

  async getMetrics(serviceIds: number[], signal?: AbortSignal) {
    const entries = await Promise.all(
      serviceIds.map(async (serviceId) => [
        serviceId,
        await apiService.get<ServiceMetric[]>(
          `/api/services/${serviceId}/metrics`,
          { signal },
        ),
      ] as const),
    )

    return Object.fromEntries(entries) as Record<number, ServiceMetric[]>
  },

  async getRecentLogs(services: Service[], signal?: AbortSignal) {
    const logsByService = await Promise.all(
      services.map(async (service) => {
        const logs = await apiService.get<LogEventResponse[]>(
          `/api/services/${service.id}/logs`,
          { signal },
        )

        return logs.map((log): LogEvent => ({
          ...log,
          serviceId: service.id,
          serviceName: service.name,
        }))
      }),
    )

    return logsByService
      .flat()
      .sort((left, right) => Date.parse(right.timestampUtc) - Date.parse(left.timestampUtc))
      .slice(0, 20)
  },
}
