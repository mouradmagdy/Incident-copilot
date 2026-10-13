import { useMutation } from '@tanstack/react-query'
import { useState } from 'react'
import { chatApi } from '../../services/chat-api'
import type { ChatSource } from '../../types/api'

export interface ChatMessage {
  id: number
  role: 'user' | 'assistant'
  content: string
  sources?: ChatSource[]
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

          return current.map((item) =>
            item.id === assistantId
              ? { ...item, sources: event.sources }
              : item,
          )
        })
      })
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

  return {
    messages,
    sendMessage,
    isPending: mutation.isPending,
    isError: mutation.isError,
  }
}
