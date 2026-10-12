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
    mutationFn: chatApi.sendMessage,
    onSuccess: (response) => {
      setMessages((current) => [
        ...current,
        {
          id: nextMessageId++,
          role: 'assistant',
          content: response.content,
          sources: response.sources ?? [],
        },
      ])
    },
  })

  function sendMessage(message: string) {
    const trimmedMessage = message.trim()
    if (!trimmedMessage || mutation.isPending) return false

    setMessages((current) => [
      ...current,
      { id: nextMessageId++, role: 'user', content: trimmedMessage },
    ])
    mutation.mutate(trimmedMessage)
    return true
  }

  return {
    messages,
    sendMessage,
    isPending: mutation.isPending,
    isError: mutation.isError,
  }
}
