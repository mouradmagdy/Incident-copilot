import { Bot, Moon, RefreshCw, Sun } from 'lucide-react'
import { useUiStore, type TimeRange } from '../../stores/ui-store'
import { Button } from '../ui/button'

const timeRanges: Array<{ value: TimeRange; label: string }> = [
  { value: '1h', label: 'Last 1h' },
  { value: '6h', label: 'Last 6h' },
  { value: '24h', label: 'Last 24h' },
  { value: '7d', label: 'Last 7d' },
]

export function DashboardHeader({
  isRefreshing,
  onRefresh,
}: {
  isRefreshing: boolean
  onRefresh: () => void
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
    <header className="overview-header">
      <div>
        <h1>System Overview</h1>
        <p>Monitor the health and performance of your services in real time.</p>
      </div>

      <div className="header-actions">
        <div className="time-range" aria-label="Telemetry time range">
          {timeRanges.map((range) => (
            <button
              key={range.value}
              type="button"
              className={timeRange === range.value ? 'active' : ''}
              onClick={() => setTimeRange(range.value)}
            >
              {range.label}
            </button>
          ))}
        </div>
        <Button variant="outline" size="icon" type="button" aria-label="Refresh telemetry" onClick={onRefresh}>
          <RefreshCw size={15} className={isRefreshing ? 'spin' : ''} />
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
        {!assistantOpen && (
          <Button
            variant="outline"
            size="icon"
            type="button"
            aria-label="Open AI assistant"
            onClick={() => setAssistantOpen(true)}
          >
            <Bot size={16} />
          </Button>
        )}
      </div>
    </header>
  )
}
