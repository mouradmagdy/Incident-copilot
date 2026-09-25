import { Database } from 'lucide-react'
import type { SimulationScenario } from '../../types/api'

const scenarios: Array<{ value: SimulationScenario; label: string }> = [
  { value: 'Healthy', label: 'Healthy' },
  { value: 'DatabaseConnectionExhaustion', label: 'DB pressure' },
]

export function ScenarioSwitcher({
  scenario,
  isPending,
  onChange,
}: {
  scenario: SimulationScenario
  isPending: boolean
  onChange: (scenario: SimulationScenario) => void
}) {
  return (
    <div className="scenario-bar">
      <div>
        <Database size={14} />
        <span>Development scenario</span>
      </div>
      <div className="scenario-switch" role="group" aria-label="Simulation scenario">
        {scenarios.map((item) => (
          <button
            key={item.value}
            type="button"
            className={scenario === item.value ? 'active' : ''}
            disabled={isPending}
            onClick={() => onChange(item.value)}
          >
            {item.label}
          </button>
        ))}
      </div>
    </div>
  )
}
