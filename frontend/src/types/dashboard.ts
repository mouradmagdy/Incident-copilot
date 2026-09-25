import type { Service, ServiceMetric } from './api'

export interface ServiceHealthRow {
  service: Service
  metric: ServiceMetric | null
  degraded: boolean
}

export interface DashboardSummary {
  degradedCount: number
  requestRate: number
  errorRate: number
  p95Latency: number
}
