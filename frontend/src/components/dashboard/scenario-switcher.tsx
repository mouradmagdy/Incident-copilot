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
    <div className="inline-flex h-8 items-center rounded-[7px] border border-border bg-surface p-[3px]" role="group" aria-label="Development scenario" title="Development scenario">
      <Database className="mx-1 shrink-0 text-muted" size={13} aria-hidden="true" />
      {scenarios.map((item) => (
        <button
          key={item.value}
          type="button"
          className={cn(
            'cursor-pointer rounded-[5px] border-0 bg-transparent px-2 py-1.5 text-[8px] text-muted disabled:cursor-wait disabled:opacity-60',
            scenario === item.value && 'bg-primary-soft font-semibold text-primary-text',
          )}
          disabled={isPending}
          onClick={() => onChange(item.value)}
        >
          {item.label}
        </button>
      ))}
    </div>
  )
}
