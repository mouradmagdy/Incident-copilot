import { ApiError } from './base-api-service'
import type { ChatSource } from '../types/api'

export type ChatStreamEvent =
  | { type: 'delta'; content: string }
  | { type: 'sources'; sources: ChatSource[] }

export const chatApi = {
  async sendMessage(
    message: string,
    onEvent: (event: ChatStreamEvent) => void,
  ) {
    const response = await fetch('/api/chat', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Accept: 'application/x-ndjson',
      },
      body: JSON.stringify({ message }),
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
}
