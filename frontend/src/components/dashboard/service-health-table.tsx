import {
  flexRender,
  getCoreRowModel,
  useReactTable,
  type ColumnDef,
} from '@tanstack/react-table'
import { useMemo } from 'react'
import { Link } from 'react-router-dom'
import type { ServiceHealthRow } from '../../types/dashboard'
import { formatNumber } from '../../utils/formatters'
import { Badge } from '../ui/badge'
import { Button } from '../ui/button'
import { Card } from '../ui/card'
import { CardHeader } from '../ui/card-header'

export function ServiceHealthTable({ rows }: { rows: ServiceHealthRow[] }) {
  const columns = useMemo<ColumnDef<ServiceHealthRow>[]>(() => [
    {
      header: 'Service',
      cell: ({ row }) => <strong className="font-medium text-text-strong">{row.original.service.name}</strong>,
    },
    {
      header: 'Status',
      cell: ({ row }) => (
        <Badge className={row.original.degraded ? 'text-danger' : 'text-success'}>
          <span className="size-[7px] rounded-full bg-current" />
          {row.original.metric
            ? row.original.degraded ? 'Degraded' : 'Healthy'
            : 'Waiting'}
        </Badge>
      ),
    },
    {
      header: 'Requests / sec',
      cell: ({ row }) => row.original.metric
        ? formatNumber(row.original.metric.requestsPerSecond)
        : '—',
    },
    {
      header: 'Error rate',
      cell: ({ row }) => row.original.metric
        ? <span className={row.original.degraded ? 'text-danger' : ''}>
            {formatNumber(row.original.metric.errorRatePercent, 1)}%
          </span>
        : '—',
    },
    {
      header: 'P95 latency',
      cell: ({ row }) => row.original.metric
        ? `${formatNumber(row.original.metric.p95LatencyMs)} ms`
        : '—',
    },
    {
      header: 'DB connections',
      cell: ({ row }) => row.original.metric
        ? formatNumber(row.original.metric.activeDbConnections)
        : '—',
    },
  ], [])

  const table = useReactTable({
    data: rows,
    columns,
    getCoreRowModel: getCoreRowModel(),
  })

  return (
    <Card className="flex h-full min-w-0 flex-col overflow-hidden">
      <CardHeader>
        <strong>Service Health</strong>
        <Button asChild variant="outline" size="sm"><Link to="/services">View all services</Link></Button>
      </CardHeader>
      <div className="flex min-h-0 flex-1 px-2.5 pb-2">
        <table className="service-health-table h-full w-full table-fixed border-collapse text-left text-[9px] whitespace-nowrap [&_td]:overflow-hidden [&_td]:border-t [&_td]:border-border-subtle [&_td]:px-1.5 [&_td]:py-1.5 [&_td]:text-ellipsis [&_td]:text-text [&_th]:overflow-hidden [&_th]:px-1.5 [&_th]:py-1 [&_th]:text-ellipsis [&_th]:font-medium [&_th]:text-muted">
          <thead>
            {table.getHeaderGroups().map((headerGroup) => (
              <tr key={headerGroup.id}>
                {headerGroup.headers.map((header) => (
                  <th key={header.id}>
                    {header.isPlaceholder ? null : flexRender(header.column.columnDef.header, header.getContext())}
                  </th>
                ))}
              </tr>
            ))}
          </thead>
          <tbody>
            {table.getRowModel().rows.map((row) => (
              <tr key={row.id}>
                {row.getVisibleCells().map((cell) => (
                  <td key={cell.id}>{flexRender(cell.column.columnDef.cell, cell.getContext())}</td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </Card>
  )
}
