import { DashboardHeader } from '../components/dashboard/dashboard-header'
import { RecentIncidents } from '../components/dashboard/recent-incidents'
import { RecentLogs } from '../components/dashboard/recent-logs'
import { ServiceHealthTable } from '../components/dashboard/service-health-table'
import { SummaryGrid } from '../components/dashboard/summary-grid'
import { TelemetryChart } from '../components/dashboard/telemetry-chart'
import { useDashboardData } from '../hooks/dashboard/use-dashboard-data'
import { useDashboardViewModel } from '../hooks/dashboard/use-dashboard-view-model'
import { useSimulationScenario } from '../hooks/simulation/use-simulation-scenario'

export function OverviewPage() {
  const { services, serviceIds, servicesQuery, metricsQuery, logsQuery, incidentsQuery, refresh } = useDashboardData()
  const { scenarioQuery, scenarioMutation } = useSimulationScenario()
  const { serviceRows, summary, telemetrySeries } = useDashboardViewModel(services, metricsQuery.data)

  const hasError = servicesQuery.isError || metricsQuery.isError || logsQuery.isError || incidentsQuery.isError
  const isRefreshing = metricsQuery.isFetching

  return (
    <div className="dashboard-container flex min-h-full min-w-0 flex-col px-4 pt-2.5 pb-4 max-[720px]:px-3">
      <DashboardHeader
        isRefreshing={isRefreshing}
        onRefresh={refresh}
        scenario={scenarioQuery.data?.scenario}
        isScenarioPending={scenarioMutation.isPending}
        onScenarioChange={(scenario) => scenarioMutation.mutate(scenario)}
      />

      {hasError && (
        <div className="mb-2.5 rounded-[7px] border border-danger/35 bg-danger-soft px-[11px] py-[9px] text-[10px] text-danger" role="alert">
          The dashboard could not reach the API. Confirm it is running on port 5087.
        </div>
      )}

      <SummaryGrid serviceCount={services.length} summary={summary} />

      <section className="dashboard-telemetry mt-2 grid flex-[1.3_1_170px] grid-cols-2 gap-2" aria-label="Telemetry trends">
        <TelemetryChart title="Request Rate" dataKey="requestRate" data={telemetrySeries} color="blue" />
        <TelemetryChart title="Error Rate (%)" dataKey="errorRate" data={telemetrySeries} color="red" />
      </section>

      <section className="dashboard-health mt-2 grid flex-[1_1_155px] grid-cols-[minmax(0,1.8fr)_minmax(220px,.9fr)] gap-2">
        <ServiceHealthTable rows={serviceRows} />
        <RecentIncidents incidents={incidentsQuery.data ?? []} isLoading={incidentsQuery.isPending} />
      </section>

      <RecentLogs logs={logsQuery.data ?? []} isLoading={logsQuery.isPending && serviceIds.length > 0} />
    </div>
  )
}
