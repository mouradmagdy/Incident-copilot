import { useMutation } from '@tanstack/react-query'
import { useState } from 'react'
import { chatApi } from '../../services/chat-api'

export interface ChatMessage {
  id: number
  role: 'user' | 'assistant'
  content: string
}

let nextMessageId = 1

export function useChat() {
  const [messages, setMessages] = useState<ChatMessage[]>([])

  const mutation = useMutation({
    mutationFn: chatApi.sendMessage,
    onSuccess: (response) => {
      setMessages((current) => [
        ...current,
        { id: nextMessageId++, role: 'assistant', content: response.content },
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
