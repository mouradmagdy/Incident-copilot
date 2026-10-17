import { Check, Copy } from 'lucide-react'
import { useState } from 'react'
import type { LogEvent } from '../../types/api'
import { getLogLevel } from '../../utils/formatters'
import { DetailPanel } from '../common/detail-panel'
import { Button } from '../ui/button'

export function LogDetailsPanel({ log, onClose }: { log: LogEvent; onClose: () => void }) {
  const level = getLogLevel(log.level)
  const [copied, setCopied] = useState(false)
  const rawLog = JSON.stringify(log, null, 2)

  async function copyTraceId() {
    if (!log.traceId) return
    await navigator.clipboard.writeText(log.traceId)
    setCopied(true)
    window.setTimeout(() => setCopied(false), 1_500)
  }

  return (
    <DetailPanel
      title="Log details"
      subtitle={log.serviceName}
      badge={<LogLevelBadge level={level} />}
      onClose={onClose}
    >
      <dl className="grid grid-cols-2 gap-x-4 gap-y-4">
        <DetailItem label="Timestamp" value={new Date(log.timestampUtc).toLocaleString()} />
        <DetailItem label="Level" value={level} />
        <DetailItem label="Service" value={log.serviceName} />
        <DetailItem label="Event ID" value={String(log.id)} />
        <div className="col-span-2">
          <dt className="text-[9px] text-muted">Message</dt>
          <dd className="mt-1 text-[11px] leading-relaxed text-text-strong">{log.message}</dd>
        </div>
        <div className="col-span-2">
          <dt className="text-[9px] text-muted">Trace ID</dt>
          <dd className="mt-1 flex min-w-0 items-center gap-1.5">
            <code className="min-w-0 flex-1 truncate rounded bg-surface-muted px-2 py-1.5 text-[9px] text-text">{log.traceId ?? 'Not available'}</code>
            {log.traceId && (
              <Button variant="outline" size="icon" type="button" aria-label="Copy trace ID" onClick={() => void copyTraceId()}>
                {copied ? <Check size={13} /> : <Copy size={13} />}
              </Button>
            )}
          </dd>
        </div>
        {log.exceptionType && <div className="col-span-2"><DetailItem label="Exception" value={log.exceptionType} /></div>}
      </dl>

      <section className="mt-5 border-t border-border pt-4">
        <h3 className="mb-2 text-[10px] font-semibold text-text-strong">Raw event</h3>
        <pre className="max-h-72 overflow-auto rounded-lg border border-border bg-surface-muted p-3 text-[9px] leading-relaxed text-text"><code>{rawLog}</code></pre>
      </section>
    </DetailPanel>
  )
}

export function LogLevelBadge({ level }: { level: string }) {
  const className = level === 'Error' || level === 'Critical'
    ? 'bg-danger-soft text-danger'
    : level === 'Warning'
      ? 'bg-warning-soft text-warning'
      : 'bg-primary-soft text-primary-text'

  return <span className={`rounded-full px-2 py-1 text-[8px] font-semibold uppercase ${className}`}>{level}</span>
}

function DetailItem({ label, value }: { label: string; value: string }) {
  return (
    <div className="min-w-0">
      <dt className="text-[9px] text-muted">{label}</dt>
      <dd className="mt-1 break-words text-[11px] text-text-strong">{value}</dd>
    </div>
  )
}
