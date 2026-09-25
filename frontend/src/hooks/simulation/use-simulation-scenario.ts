import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { simulationApi } from '../../services/simulation-api'

export function useSimulationScenario() {
  const queryClient = useQueryClient()

  const scenarioQuery = useQuery({
    queryKey: ['simulation-scenario'],
    queryFn: ({ signal }) => simulationApi.getScenario(signal),
    retry: false,
  })

  const scenarioMutation = useMutation({
    mutationFn: simulationApi.setScenario,
    onSuccess: (scenario) => {
      queryClient.setQueryData(['simulation-scenario'], scenario)
      void queryClient.invalidateQueries({ queryKey: ['service-metrics'] })
      void queryClient.invalidateQueries({ queryKey: ['recent-logs'] })
    },
  })

  return { scenarioQuery, scenarioMutation }
}
