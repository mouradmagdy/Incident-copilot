import type { ReactNode } from 'react'
import { cn } from '../../utils/classnames'
import { Card } from '../ui/card'

export function MetricCard({
  label,
  value,
  hint,
  icon,
  tone = 'blue',
}: {
  label: string
  value: string
  hint: ReactNode
  icon: ReactNode
  tone?: 'blue' | 'red'
}) {
  return (
    <Card className="grid min-h-[80px] grid-cols-[32px_minmax(0,1fr)] gap-2 px-2.5 py-2.5">
      <div className={cn('grid size-[29px] place-items-center rounded-[7px] bg-primary-soft text-primary', tone === 'red' && 'bg-danger-soft text-danger')}>{icon}</div>
      <div>
        <span className="mb-[3px] block text-[10px] text-text">{label}</span>
        <strong className="block text-lg leading-[1.1] tracking-[-.025em] text-text-strong">{value}</strong>
        <div className="mt-1 flex flex-wrap items-center gap-[5px] text-[8px] text-muted">{hint}</div>
      </div>
    </Card>
  )
}
