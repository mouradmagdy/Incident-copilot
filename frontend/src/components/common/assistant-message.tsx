import type { ChatMessage } from '../../hooks/chat/use-chat'
import { MarkdownMessage } from './markdown-message'
import { MessageSources } from './message-sources'
import type { PendingActionDecision } from '../../services/pending-actions-api'
import { PendingActionCard } from './pending-action-card'

const avatarClasses = 'grid size-[23px] shrink-0 place-items-center rounded-[5px] bg-linear-to-br from-[#4778ff] to-[#7548f8] text-[9px] font-bold text-white'

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
    <div className={`flex max-w-full items-start gap-2 ${isAssistant ? 'self-start' : 'self-end'}`}>
      {isAssistant && <span className={avatarClasses}>AI</span>}
      {isAssistant ? (
        <div className="min-w-0 max-w-[calc(100%-31px)]">
          {message.content && <MarkdownMessage content={message.content} />}
          {message.pendingAction && (
            <PendingActionCard
              action={message.pendingAction}
              isUpdating={pendingActionInFlightId === message.pendingAction.id}
              hasError={pendingActionErrorId === message.pendingAction.id}
              onDecision={onPendingActionDecision}
            />
          )}
          <MessageSources sources={message.sources} />
        </div>
      ) : (
        <p className="m-0 whitespace-pre-wrap rounded-lg bg-primary-soft px-[11px] py-[9px] text-[10px] leading-[1.45] text-primary-text">
          {message.content}
        </p>
      )}
    </div>
  )
}
