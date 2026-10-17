import { Bot, MessageSquare, Send, SquarePen, X } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { useChat } from '../../hooks/chat/use-chat'
import { useUiStore } from '../../stores/ui-store'
import { Button } from '../ui/button'
import { Input } from '../ui/input'
import { AssistantMessage } from './assistant-message'
import { ChatHistoryMenu } from './chat-history-menu'

export function AssistantPanel() {
  const [draft, setDraft] = useState('')
  const {
    messages,
    conversations,
    conversationId,
    selectConversation,
    startNewConversation,
    isLoadingConversation,
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
    <aside className="grid h-screen min-w-0 grid-rows-[auto_minmax(0,1fr)_auto] bg-surface text-text">
      <header className="flex min-h-16 items-center gap-2.5 border-b border-border px-3.5 py-3">
        <span className="grid size-7 shrink-0 place-items-center self-center rounded-[6px] bg-primary text-white">
          <Bot size={17} />
        </span>
        <div className="min-w-0 flex-1">
          <strong className="block text-sm text-text-strong">AI Assistant</strong>
          <span className="mt-0.5 block text-[11px] leading-snug text-muted">Ask about your services, logs, incidents, and metrics.</span>
        </div>
        <ChatHistoryMenu
          conversations={conversations}
          activeConversationId={conversationId}
          disabled={isPending || isLoadingConversation}
          onSelect={selectConversation}
        />
        <Button
          variant="ghost"
          size="icon"
          type="button"
          className="size-7 shrink-0 text-primary"
          aria-label="Start a new chat"
          title="New chat"
          disabled={isPending || isLoadingConversation}
          onClick={startNewConversation}
        >
          <SquarePen size={15} />
        </Button>
        <Button
          variant="ghost"
          size="icon"
          type="button"
          className="shrink-0 text-muted hover:text-text-strong"
          aria-label="Close AI assistant"
          onClick={() => setAssistantOpen(false)}
        >
          <X size={16} />
        </Button>
      </header>

      <div className="flex flex-col gap-3 overflow-y-auto p-4" aria-live="polite">
        {messages.length === 0 && (
          <div className="m-auto grid max-w-[260px] place-items-center text-center text-muted">
            <span className="mb-2.5 grid size-[38px] place-items-center rounded-[10px] bg-primary-soft text-primary"><MessageSquare size={19} /></span>
            <strong className="text-sm text-text-strong">How can I help investigate?</strong>
            <p className="mt-1 text-[11px] leading-relaxed">Ask about a service symptom or production incident.</p>
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
            <span className="grid size-7 shrink-0 place-items-center rounded-[6px] bg-primary text-[10px] font-bold text-white">AI</span>
            <p className="m-0 rounded-lg bg-surface px-3 py-2.5 text-xs leading-relaxed text-muted">Thinking…</p>
          </div>
        )}

        {isError && (
          <p className="m-0 rounded-[7px] bg-danger-soft px-2.5 py-2 text-[11px] text-danger" role="alert">
            The assistant could not complete the request. Please try again.
          </p>
        )}
      </div>

      <form className="grid grid-cols-[minmax(0,1fr)_36px] gap-2 px-3 pt-2 pb-3.5" onSubmit={submitMessage}>
        <label className="sr-only" htmlFor="assistant-input">Ask Incident Copilot</label>
        <Input
          id="assistant-input"
          value={draft}
          onChange={(event) => setDraft(event.target.value)}
          placeholder="Ask anything about your system…"
          disabled={isPending || isLoadingConversation}
          className="h-9 px-3 text-xs"
        />
        <Button className="size-9" size="icon" type="submit" aria-label="Send message" disabled={isPending || isLoadingConversation || draft.trim().length === 0}>
          <Send size={16} />
        </Button>
      </form>
    </aside>
  )
}
