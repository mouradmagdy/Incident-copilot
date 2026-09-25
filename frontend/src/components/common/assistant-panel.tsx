import { Bot, MessageSquare, Send, X } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { useChat } from '../../hooks/chat/use-chat'
import { useUiStore } from '../../stores/ui-store'
import { Button } from '../ui/button'
import { Input } from '../ui/input'

export function AssistantPanel({ open }: { open: boolean }) {
  const [draft, setDraft] = useState('')
  const { messages, sendMessage, isPending, isError } = useChat()
  const setAssistantOpen = useUiStore((state) => state.setAssistantOpen)

  function submitMessage(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (sendMessage(draft)) setDraft('')
  }

  return (
    <aside
      className={open ? 'assistant-panel' : 'assistant-panel assistant-panel--closed'}
      aria-hidden={!open}
    >
      <header className="assistant-header">
        <span className="assistant-logo"><Bot size={16} /></span>
        <div>
          <strong>AI Assistant</strong>
          <span>Ask about your services, logs, incidents, and metrics.</span>
        </div>
        <Button
          variant="ghost"
          size="icon"
          type="button"
          className="assistant-close"
          aria-label="Close AI assistant"
          onClick={() => setAssistantOpen(false)}
        >
          <X size={16} />
        </Button>
      </header>

      <div className="assistant-messages" aria-live="polite">
        {messages.length === 0 && (
          <div className="assistant-welcome">
            <span><MessageSquare size={19} /></span>
            <strong>How can I help investigate?</strong>
            <p>Ask about a service symptom or production incident.</p>
          </div>
        )}

        {messages.map((message) => (
          <div key={message.id} className={`assistant-message assistant-message--${message.role}`}>
            {message.role === 'assistant' && <span className="message-avatar">AI</span>}
            <p>{message.content}</p>
          </div>
        ))}

        {isPending && (
          <div className="assistant-message assistant-message--assistant">
            <span className="message-avatar">AI</span>
            <p className="typing-indicator">Thinking…</p>
          </div>
        )}

        {isError && (
          <p className="assistant-error" role="alert">
            The assistant could not complete the request. Please try again.
          </p>
        )}
      </div>

      <div className="suggestion-chips" aria-label="Suggested actions coming soon">
        {['Show related logs', 'Top slow queries', 'Compare with yesterday'].map((label) => (
          <button key={label} type="button" aria-disabled="true" title="Coming soon">{label}</button>
        ))}
      </div>

      <form className="assistant-form" onSubmit={submitMessage}>
        <label className="sr-only" htmlFor="assistant-input">Ask Incident Copilot</label>
        <Input
          id="assistant-input"
          value={draft}
          onChange={(event) => setDraft(event.target.value)}
          placeholder="Ask anything about your system…"
          disabled={isPending}
        />
        <Button size="icon" type="submit" aria-label="Send message" disabled={isPending || draft.trim().length === 0}>
          <Send size={16} />
        </Button>
      </form>
    </aside>
  )
}
