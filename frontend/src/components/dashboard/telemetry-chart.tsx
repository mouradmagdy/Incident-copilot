import {
  Area,
  AreaChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import type { TelemetryPoint } from '../../utils/telemetry'
import { Button } from '../ui/button'
import { Card } from '../ui/card'
import { CardContent } from '../ui/card-content'
import { CardHeader } from '../ui/card-header'
import { ChevronDown } from 'lucide-react'

export function TelemetryChart({
  title,
  dataKey,
  data,
  color,
}: {
  title: string
  dataKey: 'requestRate' | 'errorRate'
  data: TelemetryPoint[]
  color: 'blue' | 'red'
}) {
  const stroke = color === 'blue' ? 'var(--chart-blue)' : 'var(--chart-red)'
  const gradientId = `gradient-${dataKey}`

  return (
    <Card className="min-w-0">
      <CardHeader>
        <strong>{title}</strong>
        <Button variant="outline" size="sm" type="button">
          {dataKey === 'requestRate' ? 'Requests / sec' : 'Error rate'}
          <ChevronDown size={12} />
        </Button>
      </CardHeader>
      <CardContent className="h-[150px] px-2 pb-2">
        {data.length === 0 ? (
          <div className="grid h-full min-h-[76px] place-items-center text-center text-[10px] text-muted">Waiting for telemetry samples</div>
        ) : (
          <ResponsiveContainer width="100%" height="100%">
            <AreaChart data={data} margin={{ top: 8, right: 4, bottom: 0, left: -20 }}>
              <defs>
                <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor={stroke} stopOpacity={0.34} />
                  <stop offset="100%" stopColor={stroke} stopOpacity={0} />
                </linearGradient>
              </defs>
              <CartesianGrid stroke="var(--chart-grid)" vertical />
              <XAxis dataKey="label" axisLine={false} tickLine={false} tick={{ fill: 'var(--muted)', fontSize: 10 }} minTickGap={32} />
              <YAxis axisLine={false} tickLine={false} tick={{ fill: 'var(--muted)', fontSize: 10 }} width={48} />
              <Tooltip
                contentStyle={{ border: '1px solid var(--border)', borderRadius: 8, background: 'var(--surface-elevated)', color: 'var(--text-strong)', fontSize: 11 }}
                labelStyle={{ color: 'var(--muted)' }}
              />
              <Area type="monotone" dataKey={dataKey} stroke={stroke} strokeWidth={2} fill={`url(#${gradientId})`} isAnimationActive={false} />
            </AreaChart>
          </ResponsiveContainer>
        )}
      </CardContent>
    </Card>
  )
}
