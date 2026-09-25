import type { ReactNode } from 'react'
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
    <Card className="summary-card">
      <div className={`summary-icon summary-icon--${tone}`}>{icon}</div>
      <div>
        <span className="summary-label">{label}</span>
        <strong className="summary-value">{value}</strong>
        <div className="summary-hint">{hint}</div>
      </div>
    </Card>
  )
}
