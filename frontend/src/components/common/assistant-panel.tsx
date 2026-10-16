import { Bot, MessageSquare, Send, X } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { useChat } from '../../hooks/chat/use-chat'
import { useUiStore } from '../../stores/ui-store'
import { Button } from '../ui/button'
import { Input } from '../ui/input'
import { AssistantMessage } from './assistant-message'

export function AssistantPanel() {
  const [draft, setDraft] = useState('')
  const {
    messages,
    sendMessage,
    isPending,
    isError,
    decidePendingAction,
    pendingActionInFlightId,
    pendingActionErrorId,
  } = useChat()
  const setAssistantOpen = useUiStore((state) => state.setAssistantOpen)

  function submitMessage(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (sendMessage(draft)) setDraft('')
  }

  return (
    <aside className="grid h-screen min-w-0 grid-rows-[auto_minmax(0,1fr)_auto_auto] bg-sidebar">
      <header className="flex min-h-16 items-center gap-2.5 border-b border-border px-3.5 py-3">
        <span className="grid size-6 shrink-0 place-items-center self-center rounded-[5px] bg-linear-to-br from-[#4778ff] to-[#7548f8] text-white">
          <Bot size={16} />
        </span>
        <div className="min-w-0 flex-1">
          <strong className="block text-xs text-text-strong">AI Assistant</strong>
          <span className="mt-[3px] block text-[9px] text-muted">Ask about your services, logs, incidents, and metrics.</span>
        </div>
        <Button
          variant="ghost"
          size="icon"
          type="button"
          className="shrink-0"
          aria-label="Close AI assistant"
          onClick={() => setAssistantOpen(false)}
        >
          <X size={16} />
        </Button>
      </header>

      <div className="flex flex-col gap-[11px] overflow-y-auto p-3.5" aria-live="polite">
        {messages.length === 0 && (
          <div className="m-auto grid max-w-[260px] place-items-center text-center text-muted">
            <span className="mb-2.5 grid size-[38px] place-items-center rounded-[10px] bg-primary-soft text-primary"><MessageSquare size={19} /></span>
            <strong className="text-xs text-text-strong">How can I help investigate?</strong>
            <p className="mt-1 text-[9px]">Ask about a service symptom or production incident.</p>
          </div>
        )}

        {messages.map((message) => (
          <AssistantMessage
            key={message.id}
            message={message}
            pendingActionInFlightId={pendingActionInFlightId}
            pendingActionErrorId={pendingActionErrorId}
            onPendingActionDecision={decidePendingAction}
          />
        ))}

        {isPending && (
          <div className="flex max-w-full items-start gap-2 self-start">
            <span className="grid size-[23px] shrink-0 place-items-center rounded-[5px] bg-linear-to-br from-[#4778ff] to-[#7548f8] text-[9px] font-bold text-white">AI</span>
            <p className="m-0 rounded-lg bg-surface px-[11px] py-[9px] text-[10px] leading-[1.45] text-muted">Thinking…</p>
          </div>
        )}

        {isError && (
          <p className="m-0 rounded-[7px] bg-danger-soft px-2.5 py-2 text-[9px] text-danger" role="alert">
            The assistant could not complete the request. Please try again.
          </p>
        )}
      </div>

      <div className="flex gap-[7px] overflow-x-auto px-3 py-2" aria-label="Suggested actions coming soon">
        {['Show related logs', 'Top slow queries', 'Compare with yesterday'].map((label) => (
          <button className="shrink-0 cursor-default rounded-full border border-primary/35 bg-primary-soft px-2.5 py-[5px] text-[8px] text-primary-text" key={label} type="button" aria-disabled="true" title="Coming soon">{label}</button>
        ))}
      </div>

      <form className="grid grid-cols-[minmax(0,1fr)_32px] gap-[7px] px-3 pt-2 pb-3.5" onSubmit={submitMessage}>
        <label className="sr-only" htmlFor="assistant-input">Ask Incident Copilot</label>
        <Input
          id="assistant-input"
          value={draft}
          onChange={(event) => setDraft(event.target.value)}
          placeholder="Ask anything about your system…"
          disabled={isPending}
          className="h-8 px-2.5 text-[9px]"
        />
        <Button size="icon" type="submit" aria-label="Send message" disabled={isPending || draft.trim().length === 0}>
          <Send size={16} />
        </Button>
      </form>
    </aside>
  )
}
