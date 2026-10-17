import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useRef, useState } from 'react'
import { chatApi } from '../../services/chat-api'
import {
  pendingActionsApi,
  type PendingActionDecision,
} from '../../services/pending-actions-api'
import type {
  ChatDebugInfo,
  ChatSource,
  PendingAction,
} from '../../types/api'

export interface ChatMessage {
  id: number
  role: 'user' | 'assistant'
  content: string
  sources?: ChatSource[]
  debug?: ChatDebugInfo
  pendingAction?: PendingAction
}

const storedConversationKey = 'incident-copilot.conversation-id'
let nextMessageId = -1

export function useChat() {
  const queryClient = useQueryClient()
  const initialized = useRef(false)
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [conversationId, setConversationId] = useState<string | null>(null)
  const [isLoadingConversation, setIsLoadingConversation] = useState(false)

  const conversationsQuery = useQuery({
    queryKey: ['conversations'],
    queryFn: chatApi.listConversations,
  })

  async function loadConversation(id: string) {
    setIsLoadingConversation(true)
    try {
      const conversation = await chatApi.getConversation(id)
      setConversationId(conversation.id)
      localStorage.setItem(storedConversationKey, conversation.id)
      setMessages(conversation.messages.map((message) => ({
        id: message.id,
        role: message.role,
        content: message.content,
        sources: message.sources,
        debug: message.debug ?? undefined,
        pendingAction: message.pendingAction ?? undefined,
      })))
    } finally {
      setIsLoadingConversation(false)
    }
  }

  useEffect(() => {
    if (initialized.current || !conversationsQuery.data) return
    initialized.current = true

    const storedId = localStorage.getItem(storedConversationKey)
    const initial = conversationsQuery.data.find(({ id }) => id === storedId)
      ?? conversationsQuery.data[0]

    if (initial) {
      queueMicrotask(() => void loadConversation(initial.id))
    }
  }, [conversationsQuery.data])

  const mutation = useMutation({
    mutationFn: ({ message, assistantId }: { message: string; assistantId: number }) => {
      return chatApi.sendMessage(message, conversationId, (event) => {
        if (event.type === 'conversation') {
          setConversationId(event.conversationId)
          localStorage.setItem(storedConversationKey, event.conversationId)
          return
        }

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

          if (!assistantMessage) return current

          if (event.type === 'debug') {
            return current.map((item) =>
              item.id === assistantId
                ? { ...item, debug: event.debug }
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
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['conversations'] })
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
      setMessages((current) => {
        const previousAction = current.find(
          (message) => message.pendingAction?.id === action.id,
        )?.pendingAction
        const updatedMessages = current.map((message) =>
          message.pendingAction?.id === action.id
            ? {
                ...message,
                pendingAction: action,
                debug: message.debug
                  ? { ...message.debug, pendingActionState: action.status }
                  : undefined,
              }
            : message,
        )
        const assistantResponse = action.assistantResponse

        if (!assistantResponse || previousAction?.status === action.status) {
          return updatedMessages
        }

        return [
          ...updatedMessages,
          {
            id: nextMessageId--,
            role: 'assistant',
            content: assistantResponse,
          },
        ]
      })
      void queryClient.invalidateQueries({ queryKey: ['conversations'] })
    },
  })

  function sendMessage(message: string) {
    const trimmedMessage = message.trim()
    if (!trimmedMessage || mutation.isPending || isLoadingConversation) {
      return false
    }

    setMessages((current) => [
      ...current,
      { id: nextMessageId--, role: 'user', content: trimmedMessage },
    ])
    mutation.mutate({
      message: trimmedMessage,
      assistantId: nextMessageId--,
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

  function startNewConversation() {
    setConversationId(null)
    setMessages([])
    localStorage.removeItem(storedConversationKey)
  }

  return {
    messages,
    conversations: conversationsQuery.data ?? [],
    conversationId,
    selectConversation: (id: string) => void loadConversation(id),
    startNewConversation,
    isLoadingConversation,
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
