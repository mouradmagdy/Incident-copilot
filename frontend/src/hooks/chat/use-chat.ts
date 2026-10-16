import { useMutation } from '@tanstack/react-query'
import { useState } from 'react'
import { chatApi } from '../../services/chat-api'
import {
  pendingActionsApi,
  type PendingActionDecision,
} from '../../services/pending-actions-api'
import type { ChatSource, PendingAction } from '../../types/api'

export interface ChatMessage {
  id: number
  role: 'user' | 'assistant'
  content: string
  sources?: ChatSource[]
  pendingAction?: PendingAction
}

let nextMessageId = 1

export function useChat() {
  const [messages, setMessages] = useState<ChatMessage[]>([])

  const mutation = useMutation({
    mutationFn: ({ message, assistantId }: { message: string; assistantId: number }) => {
      return chatApi.sendMessage(message, (event) => {
        setMessages((current) => {
          const assistantMessage = current.find(({ id }) => id === assistantId)

          if (event.type === 'delta') {
            if (!assistantMessage) {
              return [
                ...current,
                {
                  id: assistantId,
                  role: 'assistant',
                  content: event.content,
                },
              ]
            }

            return current.map((item) =>
              item.id === assistantId
                ? { ...item, content: item.content + event.content }
                : item,
            )
          }

          if (event.type === 'pending_action') {
            if (!assistantMessage) {
              return [
                ...current,
                {
                  id: assistantId,
                  role: 'assistant',
                  content: '',
                  pendingAction: event.action,
                },
              ]
            }

            return current.map((item) =>
              item.id === assistantId
                ? { ...item, pendingAction: event.action }
                : item,
            )
          }

          return current.map((item) =>
            item.id === assistantId
              ? { ...item, sources: event.sources }
              : item,
          )
        })
      })
    },
  })

  const actionMutation = useMutation({
    mutationFn: ({
      id,
      decision,
    }: {
      id: string
      decision: PendingActionDecision
    }) => pendingActionsApi.decide(id, decision),
    onSuccess: (action) => {
      setMessages((current) =>
        current.map((message) =>
          message.pendingAction?.id === action.id
            ? { ...message, pendingAction: action }
            : message,
        ),
      )
    },
  })

  function sendMessage(message: string) {
    const trimmedMessage = message.trim()
    if (!trimmedMessage || mutation.isPending) return false

    setMessages((current) => [
      ...current,
      { id: nextMessageId++, role: 'user', content: trimmedMessage },
    ])
    mutation.mutate({
      message: trimmedMessage,
      assistantId: nextMessageId++,
    })
    return true
  }

  function decidePendingAction(
    id: string,
    decision: PendingActionDecision,
  ) {
    if (actionMutation.isPending) return
    actionMutation.mutate({ id, decision })
  }

  return {
    messages,
    sendMessage,
    isPending: mutation.isPending,
    isError: mutation.isError,
    decidePendingAction,
    pendingActionInFlightId: actionMutation.isPending
      ? actionMutation.variables?.id
      : undefined,
    pendingActionErrorId: actionMutation.isError
      ? actionMutation.variables?.id
      : undefined,
  }
}
