import { History, MessageSquare, Search } from 'lucide-react'
import { useEffect, useMemo, useRef, useState } from 'react'
import type { ConversationSummary } from '../../types/api'
import { cn } from '../../utils/classnames'
import { Button } from '../ui/button'

interface ChatHistoryMenuProps {
  conversations: ConversationSummary[]
  activeConversationId: string | null
  disabled: boolean
  onSelect: (id: string) => void
}

export function ChatHistoryMenu({
  conversations,
  activeConversationId,
  disabled,
  onSelect,
}: ChatHistoryMenuProps) {
  const [open, setOpen] = useState(false)
  const [query, setQuery] = useState('')
  const containerRef = useRef<HTMLDivElement>(null)
  const searchRef = useRef<HTMLInputElement>(null)
  const filteredConversations = useMemo(() => {
    const normalizedQuery = query.trim().toLocaleLowerCase()
    if (!normalizedQuery) return conversations

    return conversations.filter((conversation) =>
      conversation.title.toLocaleLowerCase().includes(normalizedQuery),
    )
  }, [conversations, query])

  useEffect(() => {
    if (!open) return

    searchRef.current?.focus()

    function handlePointerDown(event: PointerEvent) {
      if (!containerRef.current?.contains(event.target as Node)) {
        setOpen(false)
      }
    }

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') setOpen(false)
    }

    document.addEventListener('pointerdown', handlePointerDown)
    document.addEventListener('keydown', handleKeyDown)

    return () => {
      document.removeEventListener('pointerdown', handlePointerDown)
      document.removeEventListener('keydown', handleKeyDown)
    }
  }, [open])

  function selectConversation(id: string) {
    onSelect(id)
    setOpen(false)
  }

  return (
    <div className="relative" ref={containerRef}>
      <Button
        variant="ghost"
        size="icon"
        type="button"
        className="size-7 shrink-0 text-primary"
        aria-label="Open chat history"
        aria-expanded={open}
        title="Chat history"
        disabled={disabled}
        onClick={() => setOpen((current) => !current)}
      >
        <History size={15} />
      </Button>

      <div
        className={cn(
          'absolute top-[calc(100%+9px)] right-0 z-30 w-[min(360px,calc(100vw-76px))] origin-top-right rounded-xl border border-border bg-surface-elevated p-2 shadow-[0_18px_55px_rgb(5_12_25/28%)] transition-[opacity,transform,visibility] duration-150 ease-out',
          open
            ? 'visible translate-y-0 scale-100 opacity-100'
            : 'invisible -translate-y-1 scale-[.98] opacity-0 pointer-events-none',
        )}
        aria-hidden={!open}
      >
        <label className="flex h-8 items-center gap-2 border-b border-border px-1 text-muted">
          <Search size={14} aria-hidden="true" />
          <span className="sr-only">Search recent chats</span>
          <input
            ref={searchRef}
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Search recent chats"
            className="min-w-0 flex-1 border-0 bg-transparent text-[11px] text-text outline-none placeholder:text-muted"
          />
        </label>

        <div className="flex items-center justify-between px-1 pt-2 pb-1">
          <strong className="text-[11px] text-text-strong">All chats</strong>
          <span className="text-[10px] text-muted">{filteredConversations.length}</span>
        </div>

        <div className="max-h-[min(360px,55vh)] overflow-y-auto">
          {filteredConversations.length === 0 ? (
            <div className="grid min-h-24 place-items-center px-3 text-center text-[11px] text-muted">
              {conversations.length === 0
                ? 'No saved chats yet.'
                : 'No chats match your search.'}
            </div>
          ) : (
            filteredConversations.map((conversation) => (
              <button
                key={conversation.id}
                type="button"
                className={cn(
                  'flex w-full cursor-pointer items-center gap-2 rounded-md border-0 bg-transparent px-2 py-2 text-left text-xs text-text transition-colors hover:bg-surface-muted',
                  conversation.id === activeConversationId
                    && 'bg-primary-soft text-primary-text',
                )}
                onClick={() => selectConversation(conversation.id)}
              >
                <MessageSquare className="shrink-0 opacity-65" size={13} />
                <span className="min-w-0 flex-1 truncate">{conversation.title}</span>
                <time className="shrink-0 text-[10px] text-muted" dateTime={conversation.updatedAtUtc}>
                  {formatRelativeTime(conversation.updatedAtUtc)}
                </time>
              </button>
            ))
          )}
        </div>
      </div>
    </div>
  )
}

function formatRelativeTime(value: string) {
  const elapsedMinutes = Math.max(
    0,
    Math.floor((Date.now() - new Date(value).getTime()) / 60_000),
  )

  if (elapsedMinutes < 1) return 'now'
  if (elapsedMinutes < 60) return `${elapsedMinutes}m`

  const elapsedHours = Math.floor(elapsedMinutes / 60)
  if (elapsedHours < 24) return `${elapsedHours}h`

  const elapsedDays = Math.floor(elapsedHours / 24)
  if (elapsedDays < 7) return `${elapsedDays}d`

  return new Intl.DateTimeFormat(undefined, {
    month: 'short',
    day: 'numeric',
  }).format(new Date(value))
}
