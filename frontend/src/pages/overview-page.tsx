import { DashboardHeader } from '../components/dashboard/dashboard-header'
import { RecentIncidents } from '../components/dashboard/recent-incidents'
import { RecentLogs } from '../components/dashboard/recent-logs'
import { ScenarioSwitcher } from '../components/dashboard/scenario-switcher'
import { ServiceHealthTable } from '../components/dashboard/service-health-table'
import { SummaryGrid } from '../components/dashboard/summary-grid'
import { TelemetryChart } from '../components/dashboard/telemetry-chart'
import { useDashboardData } from '../hooks/dashboard/use-dashboard-data'
import { useDashboardViewModel } from '../hooks/dashboard/use-dashboard-view-model'
import { useSimulationScenario } from '../hooks/simulation/use-simulation-scenario'

export function OverviewPage() {
  const { services, serviceIds, servicesQuery, metricsQuery, logsQuery, refresh } = useDashboardData()
  const { scenarioQuery, scenarioMutation } = useSimulationScenario()
  const { serviceRows, summary, telemetrySeries } = useDashboardViewModel(services, metricsQuery.data)

  const hasError = servicesQuery.isError || metricsQuery.isError || logsQuery.isError
  const isRefreshing = metricsQuery.isFetching || logsQuery.isFetching

  return (
    <div className="min-w-0 px-[18px] pt-3.5 pb-7 max-[720px]:px-3 max-[720px]:pt-[15px] max-[720px]:pb-6">
      <DashboardHeader isRefreshing={isRefreshing} onRefresh={refresh} />

      {hasError && (
        <div className="mb-2.5 rounded-[7px] border border-danger/35 bg-danger-soft px-[11px] py-[9px] text-[10px] text-danger" role="alert">
          The dashboard could not reach the API. Confirm it is running on port 5087.
        </div>
      )}

      {scenarioQuery.isSuccess && (
        <ScenarioSwitcher
          scenario={scenarioQuery.data.scenario}
          isPending={scenarioMutation.isPending}
          onChange={(scenario) => scenarioMutation.mutate(scenario)}
        />
      )}

      <SummaryGrid serviceCount={services.length} summary={summary} />

      <section className="mt-2.5 grid grid-cols-2 gap-2.5 max-[940px]:grid-cols-1" aria-label="Telemetry trends">
        <TelemetryChart title="Request Rate" dataKey="requestRate" data={telemetrySeries} color="blue" />
        <TelemetryChart title="Error Rate (%)" dataKey="errorRate" data={telemetrySeries} color="red" />
      </section>

      <section className="mt-2.5 grid grid-cols-[minmax(0,1.8fr)_minmax(230px,.9fr)] gap-2.5 max-[940px]:grid-cols-1">
        <ServiceHealthTable rows={serviceRows} />
        <RecentIncidents />
      </section>

      <RecentLogs logs={logsQuery.data ?? []} isLoading={logsQuery.isPending && serviceIds.length > 0} />
    </div>
  )
}
