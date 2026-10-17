import type { ServiceMetric } from '../types/api'
import type { TimeRange } from '../stores/ui-store'
import { apiService } from './base-api-service'

interface ServiceMetricBatchResponse extends ServiceMetric {
  serviceId: number
}

export const metricsApi = {
  async get(serviceIds: number[], range: TimeRange, signal?: AbortSignal) {
    const query = new URLSearchParams({ range })
    serviceIds.forEach((id) => query.append('serviceIds', String(id)))
    const metrics = await apiService.get<ServiceMetricBatchResponse[]>(
      `/api/metrics?${query}`,
      { signal },
    )

    return metrics.reduce<Record<number, ServiceMetric[]>>((grouped, metric) => {
      const { serviceId, ...value } = metric
      ;(grouped[serviceId] ??= []).push(value)
      return grouped
    }, {})
  },
}
