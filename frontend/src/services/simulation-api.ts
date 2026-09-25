import { apiService } from './base-api-service'
import type { SimulationScenario, SimulationScenarioResponse } from '../types/api'

export const simulationApi = {
  getScenario(signal?: AbortSignal) {
    return apiService.get<SimulationScenarioResponse>(
      '/api/simulation/scenario',
      { signal },
    )
  },

  setScenario(scenario: SimulationScenario) {
    return apiService.post<SimulationScenarioResponse>(
      '/api/simulation/scenario',
      { scenario },
    )
  },
}
