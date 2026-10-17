import type { ChatMessage } from '../../hooks/chat/use-chat'
import { MarkdownMessage } from './markdown-message'
import type { PendingActionDecision } from '../../services/pending-actions-api'
import { PendingActionCard } from './pending-action-card'
import { MessageDebug } from './message-debug'

const avatarClasses = 'grid size-7 shrink-0 place-items-center rounded-[6px] bg-linear-to-br from-[#4778ff] to-[#7548f8] text-[10px] font-bold text-white'

interface AssistantMessageProps {
  message: ChatMessage
  pendingActionInFlightId?: string
  pendingActionErrorId?: string
  onPendingActionDecision: (
    id: string,
    decision: PendingActionDecision,
  ) => void
}

export function AssistantMessage({
  message,
  pendingActionInFlightId,
  pendingActionErrorId,
  onPendingActionDecision,
}: AssistantMessageProps) {
  const isAssistant = message.role === 'assistant'

  return (
    <div className={`flex max-w-full items-start gap-2.5 ${isAssistant ? 'self-start' : 'self-end'}`}>
      {isAssistant && <span className={avatarClasses}>AI</span>}
      {isAssistant ? (
        <div className="min-w-0 max-w-[calc(100%-38px)]">
          {message.content && <MarkdownMessage content={message.content} />}
          {message.pendingAction && (
            <PendingActionCard
              action={message.pendingAction}
              isUpdating={pendingActionInFlightId === message.pendingAction.id}
              hasError={pendingActionErrorId === message.pendingAction.id}
              onDecision={onPendingActionDecision}
            />
          )}
          <MessageDebug debug={message.debug} />
        </div>
      ) : (
        <p className="m-0 whitespace-pre-wrap rounded-lg bg-primary-soft px-3 py-2.5 text-xs leading-relaxed text-primary-text">
          {message.content}
        </p>
      )}
    </div>
  )
}
