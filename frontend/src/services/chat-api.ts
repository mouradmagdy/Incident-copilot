import { ApiError } from './base-api-service'
import type {
  ChatDebugInfo,
  ChatSource,
  Conversation,
  ConversationSummary,
  PendingAction,
} from '../types/api'

export type ChatStreamEvent =
  | { type: 'conversation'; conversationId: string }
  | { type: 'delta'; content: string }
  | { type: 'pending_action'; action: PendingAction }
  | { type: 'sources'; sources: ChatSource[] }
  | { type: 'debug'; debug: ChatDebugInfo }

export const chatApi = {
  async sendMessage(
    message: string,
    conversationId: string | null,
    onEvent: (event: ChatStreamEvent) => void,
  ) {
    const response = await fetch('/api/chat', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Accept: 'application/x-ndjson',
      },
      body: JSON.stringify({ message, conversationId }),
    })

    if (!response.ok) {
      throw new ApiError(
        response.status,
        `API request failed with status ${response.status}`,
      )
    }

    if (!response.body) {
      throw new Error('The chat response did not contain a readable stream.')
    }

    const reader = response.body.getReader()
    const decoder = new TextDecoder()
    let buffer = ''

    while (true) {
      const { value, done } = await reader.read()

      if (done) break

      buffer += decoder.decode(value, { stream: true })
      const lines = buffer.split('\n')
      buffer = lines.pop() ?? ''

      for (const line of lines) {
        if (line.trim()) onEvent(JSON.parse(line) as ChatStreamEvent)
      }
    }

    buffer += decoder.decode()
    if (buffer.trim()) onEvent(JSON.parse(buffer) as ChatStreamEvent)
  },

  async listConversations() {
    const response = await fetch('/api/chat/conversations')

    if (!response.ok) {
      throw new ApiError(response.status, 'Could not load conversations.')
    }

    return (await response.json()) as ConversationSummary[]
  },

  async getConversation(id: string) {
    const response = await fetch(`/api/chat/conversations/${id}`)

    if (!response.ok) {
      throw new ApiError(response.status, 'Could not load the conversation.')
    }

    return (await response.json()) as Conversation
  },
}
