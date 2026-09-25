import { useMemo } from 'react'
import type { Service, ServiceMetric } from '../../types/api'
import type { DashboardSummary, ServiceHealthRow } from '../../types/dashboard'
import { buildTelemetrySeries, isMetricDegraded } from '../../utils/telemetry'

export function useDashboardViewModel(
  services: Service[],
  metricsByService: Record<number, ServiceMetric[]> | undefined,
) {
  return useMemo(() => {
    const latestMetrics = Object.fromEntries(
      Object.entries(metricsByService ?? {}).map(([serviceId, metrics]) => [
        Number(serviceId),
        metrics[0] ?? null,
      ]),
    ) as Record<number, ServiceMetric | null>

    const serviceRows: ServiceHealthRow[] = services.map((service) => {
      const metric = latestMetrics[service.id] ?? null
      return { service, metric, degraded: isMetricDegraded(metric) }
    })

    const latestValues = serviceRows
      .map((row) => row.metric)
      .filter((metric): metric is ServiceMetric => metric !== null)

    const summary: DashboardSummary = {
      degradedCount: serviceRows.filter((row) => row.degraded).length,
      requestRate: latestValues.reduce(
        (sum, metric) => sum + metric.requestsPerSecond,
        0,
      ),
      errorRate: latestValues.length
        ? latestValues.reduce(
            (sum, metric) => sum + metric.errorRatePercent,
            0,
          ) / latestValues.length
        : 0,
      p95Latency: latestValues.length
        ? Math.max(...latestValues.map((metric) => metric.p95LatencyMs))
        : 0,
    }

    return {
      serviceRows,
      summary,
      telemetrySeries: buildTelemetrySeries(metricsByService),
    }
  }, [services, metricsByService])
}
