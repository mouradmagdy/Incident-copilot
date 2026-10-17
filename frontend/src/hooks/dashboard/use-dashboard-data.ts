import { useQuery } from '@tanstack/react-query'
import { servicesApi } from '../../services/services-api'
import { metricsApi } from '../../services/metrics-api'
import { logsApi } from '../../services/logs-api'
import { incidentsApi } from '../../services/incidents-api'
import { useUiStore } from '../../stores/ui-store'

const serviceLimit = 4

export function useDashboardData() {
  const timeRange = useUiStore((state) => state.timeRange)
  const servicesQuery = useQuery({
    queryKey: ['services'],
    queryFn: ({ signal }) => servicesApi.getServices(signal),
    staleTime: 60_000,
  })

  const services = (servicesQuery.data ?? []).slice(0, serviceLimit)
  const serviceIds = services.map((service) => service.id)

  const metricsQuery = useQuery({
    queryKey: ['service-metrics', serviceIds, timeRange],
    queryFn: ({ signal }) => metricsApi.get(serviceIds, timeRange, signal),
    enabled: serviceIds.length > 0,
    refetchInterval: 10_000,
  })

  const logsQuery = useQuery({
    queryKey: ['recent-logs', serviceIds],
    queryFn: ({ signal }) => logsApi.get({ serviceIds, pageSize: 20 }, signal),
    enabled: serviceIds.length > 0,
    refetchInterval: 10_000,
  })

  const incidentsQuery = useQuery({
    queryKey: ['recent-incidents'],
    queryFn: ({ signal }) => incidentsApi.get(5, signal),
    refetchInterval: 30_000,
  })

  return {
    services,
    serviceIds,
    servicesQuery,
    metricsQuery,
    logsQuery,
    incidentsQuery,
    refresh: () => {
      void metricsQuery.refetch()
    },
  }
}
