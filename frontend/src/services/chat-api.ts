import { apiService } from './base-api-service'
import type { ChatResponse } from '../types/api'

export const chatApi = {
  sendMessage(message: string) {
    return apiService.post<ChatResponse>('/api/chat', { message })
  },
}
