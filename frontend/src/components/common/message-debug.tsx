import { Bug, ChevronDown, FileText, Wrench } from 'lucide-react'
import type { ChatDebugInfo } from '../../types/api'

export function MessageDebug({ debug }: { debug?: ChatDebugInfo }) {
  if (!debug) return null

  return (
    <details className="group mt-1.5 rounded-md border border-border-subtle bg-surface/70 text-[8px] text-muted">
      <summary className="flex cursor-pointer list-none items-center gap-1 px-2 py-1.5 font-semibold text-text [&::-webkit-details-marker]:hidden">
        <Bug size={11} aria-hidden="true" />
        Developer details
        <ChevronDown className="ml-auto transition-transform group-open:rotate-180" size={11} aria-hidden="true" />
      </summary>
      <div className="space-y-2 border-t border-border-subtle px-2 py-2">
        <dl className="grid grid-cols-2 gap-x-3 gap-y-1">
          <dt>LLM rounds</dt><dd className="text-right font-medium text-text">{debug.llmRoundCount}</dd>
          <dt>Total latency</dt><dd className="text-right font-medium text-text">{Math.round(debug.totalLatencyMs)} ms</dd>
          <dt>Pending action</dt><dd className="text-right font-medium text-text">{debug.pendingActionState}</dd>
        </dl>

        <div>
          <strong className="mb-1 block text-text">Retrieved sources</strong>
          {debug.sources.length === 0 ? (
            <span>None passed the relevance threshold.</span>
          ) : (
            <ul className="space-y-1">
              {debug.sources.map((source) => (
                <li className="flex items-start gap-1" key={`${source.chunkId}-${source.fileName}-${source.section ?? ''}`}>
                  <FileText className="mt-px shrink-0" size={10} aria-hidden="true" />
                  <span>{source.fileName}{source.section ? ` · ${source.section}` : ''}</span>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div>
          <strong className="mb-1 block text-text">Tool calls</strong>
          {debug.toolCalls.length === 0 ? (
            <span>None</span>
          ) : (
            <ul className="space-y-1">
              {debug.toolCalls.map((tool, index) => (
                <li className="flex items-center gap-1" key={`${tool.name}-${index}`}>
                  <Wrench size={10} aria-hidden="true" />
                  <code className="text-text">{tool.name}</code>
                  <span className="ml-auto">
                    {tool.durationMs === null ? 'not executed' : `${Math.round(tool.durationMs)} ms`}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </details>
  )
}
