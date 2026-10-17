import { apiService } from './base-api-service'
import type { Service } from '../types/api'

export const servicesApi = {
  getServices(signal?: AbortSignal) {
    return apiService.get<Service[]>('/api/services', { signal })
  },
}
