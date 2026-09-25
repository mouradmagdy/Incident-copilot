import type { ServiceMetric } from '../types/api'
import { formatTime } from './formatters'

export interface TelemetryPoint {
  timestamp: number
  label: string
  requestRate: number
  errorRate: number
}

export function isMetricDegraded(metric: ServiceMetric | null | undefined) {
  return Boolean(
    metric && (
      metric.p95LatencyMs >= 1_000 ||
      metric.errorRatePercent >= 5 ||
      metric.activeDbConnections >= 90
    ),
  )
}

export function buildTelemetrySeries(
  metricsByService: Record<number, ServiceMetric[]> | undefined,
) {
  if (!metricsByService) return []

  const buckets = new Map<number, {
    requestRate: number
    errorRate: number
    samples: number
  }>()

  Object.values(metricsByService).forEach((metrics) => {
    metrics.forEach((metric) => {
      const timestamp = Date.parse(metric.timestampUtc)
      const bucket = buckets.get(timestamp) ?? {
        requestRate: 0,
        errorRate: 0,
        samples: 0,
      }

      bucket.requestRate += metric.requestsPerSecond
      bucket.errorRate += metric.errorRatePercent
      bucket.samples += 1
      buckets.set(timestamp, bucket)
    })
  })

  return Array.from(buckets.entries())
    .sort(([left], [right]) => left - right)
    .slice(-48)
    .map(([timestamp, bucket]): TelemetryPoint => ({
      timestamp,
      label: formatTime(timestamp),
      requestRate: Math.round(bucket.requestRate),
      errorRate: Number((bucket.errorRate / bucket.samples).toFixed(2)),
    }))
}
