import { useQuery } from '@tanstack/react-query'
import { servicesApi } from '../../services/services-api'

const serviceLimit = 4

export function useDashboardData() {
  const servicesQuery = useQuery({
    queryKey: ['services'],
    queryFn: ({ signal }) => servicesApi.getServices(signal),
    staleTime: 60_000,
  })

  const services = (servicesQuery.data ?? []).slice(0, serviceLimit)
  const serviceIds = services.map((service) => service.id)

  const metricsQuery = useQuery({
    queryKey: ['service-metrics', serviceIds],
    queryFn: ({ signal }) => servicesApi.getMetrics(serviceIds, signal),
    enabled: serviceIds.length > 0,
    refetchInterval: 10_000,
  })

  const logsQuery = useQuery({
    queryKey: ['recent-logs', serviceIds],
    queryFn: ({ signal }) => servicesApi.getRecentLogs(services, signal),
    enabled: serviceIds.length > 0,
    refetchInterval: 10_000,
  })

  return {
    services,
    serviceIds,
    servicesQuery,
    metricsQuery,
    logsQuery,
    refresh: () => {
      void metricsQuery.refetch()
      void logsQuery.refetch()
    },
  }
}
