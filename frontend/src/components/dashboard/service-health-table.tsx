import {
  flexRender,
  getCoreRowModel,
  useReactTable,
  type ColumnDef,
} from '@tanstack/react-table'
import { MoreHorizontal } from 'lucide-react'
import { useMemo } from 'react'
import type { ServiceHealthRow } from '../../types/dashboard'
import { formatNumber } from '../../utils/formatters'
import { Badge } from '../ui/badge'
import { Button } from '../ui/button'
import { Card, CardHeader } from '../ui/card'

export function ServiceHealthTable({ rows }: { rows: ServiceHealthRow[] }) {
  const columns = useMemo<ColumnDef<ServiceHealthRow>[]>(() => [
    {
      header: 'Service',
      cell: ({ row }) => <strong className="service-name">{row.original.service.name}</strong>,
    },
    {
      header: 'Status',
      cell: ({ row }) => (
        <Badge className={row.original.degraded ? 'status-badge status-badge--danger' : 'status-badge'}>
          <span className="status-dot" />
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
        ? <span className={row.original.degraded ? 'danger-text' : ''}>
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
    {
      id: 'actions',
      cell: () => <MoreHorizontal size={15} className="table-more" />,
    },
  ], [])

  const table = useReactTable({
    data: rows,
    columns,
    getCoreRowModel: getCoreRowModel(),
  })

  return (
    <Card className="table-card">
      <CardHeader>
        <strong>Service Health</strong>
        <Button variant="outline" size="sm" type="button">View all services</Button>
      </CardHeader>
      <div className="table-scroll">
        <table className="health-table">
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
