import { Bot, Moon, RefreshCw, Sun } from 'lucide-react'
import { useUiStore, type TimeRange } from '../../stores/ui-store'
import { cn } from '../../utils/classnames'
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
    <header className="mb-3 flex min-h-11 items-center justify-between gap-5 max-[940px]:flex-col max-[940px]:items-start">
      <div>
        <h1 className="mb-[3px] text-xl leading-[1.1] tracking-[-.025em] text-text-strong">System Overview</h1>
        <p className="text-[11px] text-muted">Monitor the health and performance of your services in real time.</p>
      </div>

      <div className="flex items-center gap-[7px] max-[940px]:w-full max-[500px]:flex-wrap">
        <div className="inline-flex rounded-[7px] border border-border bg-surface p-[3px] max-[940px]:flex-1 max-[940px]:overflow-x-auto" aria-label="Telemetry time range">
          {timeRanges.map((range) => (
            <button
              key={range.value}
              type="button"
              className={cn(
                'cursor-pointer rounded-[5px] border-0 bg-transparent px-[9px] py-1.5 text-[9px] text-muted',
                timeRange === range.value && 'bg-primary-soft font-semibold text-primary-text',
              )}
              onClick={() => setTimeRange(range.value)}
            >
              {range.label}
            </button>
          ))}
        </div>
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
