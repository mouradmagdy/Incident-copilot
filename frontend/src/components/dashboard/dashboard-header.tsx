import { Bot, ChevronDown, Moon, RefreshCw, Sun } from 'lucide-react'
import { useUiStore, type TimeRange } from '../../stores/ui-store'
import type { SimulationScenario } from '../../types/api'
import { Button } from '../ui/button'
import { ScenarioSwitcher } from './scenario-switcher'

const timeRanges: Array<{ value: TimeRange; label: string }> = [
  { value: '1h', label: 'Last 1h' },
  { value: '6h', label: 'Last 6h' },
  { value: '24h', label: 'Last 24h' },
  { value: '7d', label: 'Last 7d' },
]

export function DashboardHeader({
  isRefreshing,
  onRefresh,
  scenario,
  isScenarioPending,
  onScenarioChange,
}: {
  isRefreshing: boolean
  onRefresh: () => void
  scenario?: SimulationScenario
  isScenarioPending: boolean
  onScenarioChange: (scenario: SimulationScenario) => void
}) {
  const {
    theme,
    timeRange,
    assistantOpen,
    toggleTheme,
    setTimeRange,
    setAssistantOpen,
  } = useUiStore()

  return (
    <header className="dashboard-header mb-2 flex min-h-10 flex-wrap items-center gap-x-5 gap-y-2">
      <div className="min-w-[190px] flex-1">
        <h1 className="mb-0.5 text-lg leading-[1.1] tracking-[-.025em] text-text-strong">System Overview</h1>
        <p className="text-[10px] text-muted">Monitor the health and performance of your services in real time.</p>
      </div>

      <div className="dashboard-header-controls ml-auto flex min-w-0 flex-wrap items-center justify-end gap-1.5">
        {scenario && (
          <ScenarioSwitcher
            scenario={scenario}
            isPending={isScenarioPending}
            onChange={onScenarioChange}
          />
        )}
        <label className="relative">
          <span className="sr-only">Telemetry time range</span>
          <select
            value={timeRange}
            onChange={(event) => setTimeRange(event.target.value as TimeRange)}
            className="h-8 cursor-pointer appearance-none rounded-[7px] border border-border bg-surface py-0 pr-7 pl-2.5 text-[9px] font-medium text-text outline-none focus-visible:border-primary"
          >
            {timeRanges.map((range) => (
              <option key={range.value} value={range.value}>{range.label}</option>
            ))}
          </select>
          <ChevronDown className="pointer-events-none absolute top-1/2 right-2 -translate-y-1/2 text-muted" size={12} aria-hidden="true" />
        </label>
        <Button variant="outline" size="icon" type="button" aria-label="Refresh telemetry" onClick={onRefresh}>
          <RefreshCw size={15} className={isRefreshing ? 'animate-spin' : ''} />
        </Button>
        <Button
          variant="ghost"
          size="icon"
          type="button"
          aria-label={`Switch to ${theme === 'light' ? 'dark' : 'light'} mode`}
          onClick={toggleTheme}
        >
          {theme === 'light' ? <Moon size={16} /> : <Sun size={16} />}
        </Button>
        <Button
          variant={assistantOpen ? 'default' : 'outline'}
          size="icon"
          type="button"
          aria-label={assistantOpen ? 'Close AI assistant' : 'Open AI assistant'}
          aria-pressed={assistantOpen}
          onClick={() => setAssistantOpen(!assistantOpen)}
        >
          <Bot size={16} />
        </Button>
      </div>
    </header>
  )
}
