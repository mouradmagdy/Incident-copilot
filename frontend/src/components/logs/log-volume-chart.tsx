import { Bar, BarChart, CartesianGrid, Legend, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import type { TimeRange } from '../../stores/ui-store'
import type { LogVolumeBucket } from '../../types/api'
import { Card } from '../ui/card'

export function LogVolumeChart({
  buckets,
  range,
  totalCount,
  isLoading,
}: {
  buckets: LogVolumeBucket[]
  range: TimeRange
  totalCount: number
  isLoading: boolean
}) {
  const formatter = new Intl.DateTimeFormat('en-US', range === '7d'
    ? { weekday: 'short' }
    : { hour: '2-digit', minute: '2-digit' })
  const data = buckets.map((bucket) => ({
    label: formatter.format(new Date(bucket.bucketStartUtc)),
    info: bucket.infoCount,
    warning: bucket.warningCount,
    error: bucket.errorCount,
  }))

  return (
    <Card className="min-h-[178px] p-3">
      <div className="mb-1 flex items-center justify-between">
        <h2 className="text-[12px] font-semibold text-text-strong">Log volume</h2>
        <span className="text-[9px] text-muted">{totalCount} matching events</span>
      </div>
      <div className="h-[142px] min-h-[142px] w-full">
        {isLoading ? (
          <div className="grid h-full place-items-center text-[10px] text-muted">Loading log volume...</div>
        ) : data.length === 0 ? (
          <div className="grid h-full place-items-center rounded-md border border-dashed border-border text-[10px] text-muted">No log volume for this time range</div>
        ) : (
          <ResponsiveContainer width="100%" height="100%" minWidth={1} minHeight={142}>
            <BarChart data={data} margin={{ top: 8, right: 4, bottom: 0, left: -28 }}>
              <CartesianGrid stroke="var(--chart-grid)" vertical={false} />
              <XAxis dataKey="label" axisLine={false} tickLine={false} tick={{ fill: 'var(--muted)', fontSize: 8 }} minTickGap={24} />
              <YAxis allowDecimals={false} axisLine={false} tickLine={false} tick={{ fill: 'var(--muted)', fontSize: 8 }} />
              <Tooltip contentStyle={{ border: '1px solid var(--border)', borderRadius: 8, background: 'var(--surface-elevated)', color: 'var(--text-strong)', fontSize: 10 }} />
              <Legend wrapperStyle={{ fontSize: 9 }} iconSize={7} />
              <Bar dataKey="info" name="Info" stackId="logs" fill="var(--chart-blue)" isAnimationActive={false} />
              <Bar dataKey="warning" name="Warning" stackId="logs" fill="var(--warning)" isAnimationActive={false} />
              <Bar dataKey="error" name="Error" stackId="logs" fill="var(--danger)" radius={[2, 2, 0, 0]} isAnimationActive={false} />
            </BarChart>
          </ResponsiveContainer>
        )}
      </div>
    </Card>
  )
}
