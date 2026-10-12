import { Database } from 'lucide-react'
import type { SimulationScenario } from '../../types/api'
import { cn } from '../../utils/classnames'

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
    <div className="mb-2.5 flex items-center justify-between gap-3 rounded-lg border border-border bg-surface py-1.5 pr-[7px] pl-[11px] max-[500px]:flex-col max-[500px]:items-stretch">
      <div className="flex items-center gap-[7px] text-[10px] text-muted">
        <Database size={14} />
        <span>Development scenario</span>
      </div>
      <div className="inline-flex rounded-[7px] border border-border bg-surface p-[3px] max-[500px]:grid max-[500px]:grid-cols-2" role="group" aria-label="Simulation scenario">
        {scenarios.map((item) => (
          <button
            key={item.value}
            type="button"
            className={cn(
              'cursor-pointer rounded-[5px] border-0 bg-transparent px-[9px] py-1.5 text-[9px] text-muted disabled:cursor-wait disabled:opacity-60',
              scenario === item.value && 'bg-primary-soft font-semibold text-primary-text',
            )}
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
