import type { ChatMessage } from '../../hooks/chat/use-chat'
import { MarkdownMessage } from './markdown-message'
import { MessageSources } from './message-sources'

const avatarClasses = 'grid size-[23px] shrink-0 place-items-center rounded-[5px] bg-linear-to-br from-[#4778ff] to-[#7548f8] text-[9px] font-bold text-white'

export function AssistantMessage({ message }: { message: ChatMessage }) {
  const isAssistant = message.role === 'assistant'

  return (
    <div className={`flex max-w-full items-start gap-2 ${isAssistant ? 'self-start' : 'self-end'}`}>
      {isAssistant && <span className={avatarClasses}>AI</span>}
      {isAssistant ? (
        <div className="min-w-0 max-w-[calc(100%-31px)]">
          <MarkdownMessage content={message.content} />
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
