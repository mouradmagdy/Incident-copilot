import type { Incident } from '../types/api'
import { apiService } from './base-api-service'

export const incidentsApi = {
  get(take = 20, signal?: AbortSignal) {
    return apiService.get<Incident[]>(`/api/incidents?take=${take}`, { signal })
  },

  updateStatus(id: number, status: Incident['status']) {
    return apiService.patch<Incident>(`/api/incidents/${id}/status`, { status })
  },
}
