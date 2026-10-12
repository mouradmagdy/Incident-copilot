import { FileText } from 'lucide-react'
import type { ChatSource } from '../../types/api'

export function MessageSources({ sources }: { sources?: ChatSource[] }) {
  if (!sources?.length) return null

  return (
    <section
      className="mt-[5px] rounded-md border border-border-subtle bg-surface/70 px-2 py-1.5 text-[8px] text-muted"
      aria-label="Sources"
    >
      <strong className="mb-1 block text-[8px] font-semibold text-text">Sources</strong>
      <ul className="flex list-none flex-wrap gap-1 p-0">
        {sources.map((source) => (
          <li
            className="inline-flex min-w-0 max-w-full items-center gap-[3px] rounded-sm bg-surface-muted px-[5px] py-[3px]"
            key={`${source.fileName}-${source.section ?? ''}`}
          >
            <FileText className="shrink-0" size={11} aria-hidden="true" />
            <span className="truncate font-semibold text-text">{source.fileName}</span>
            {source.section?.trim() && (
              <span className="truncate before:mr-[3px] before:content-['·']">
                {source.section}
              </span>
            )}
          </li>
        ))}
      </ul>
    </section>
  )
}
