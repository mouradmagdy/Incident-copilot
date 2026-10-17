import type { LogEvent, LogEventPage, LogVolumeBucket } from '../types/api'
import type { TimeRange } from '../stores/ui-store'
import { apiService } from './base-api-service'

export interface LogQuery {
  serviceIds?: number[]
  page?: number
  pageSize?: number
  range?: TimeRange
  level?: string
  search?: string
}

export const logsApi = {
  getPage(options: LogQuery = {}, signal?: AbortSignal) {
    const query = buildFilterQuery(options)
    query.set('page', String(options.page ?? 1))
    query.set('pageSize', String(options.pageSize ?? 20))
    return apiService.get<LogEventPage>(`/api/logs?${query}`, { signal })
  },

  async get(options: LogQuery = {}, signal?: AbortSignal): Promise<LogEvent[]> {
    const response = await this.getPage(options, signal)
    return response.items
  },

  getVolume(options: LogQuery = {}, signal?: AbortSignal) {
    const query = buildFilterQuery(options)
    return apiService.get<LogVolumeBucket[]>(`/api/logs/volume?${query}`, { signal })
  },
}

function buildFilterQuery(options: LogQuery) {
  const query = new URLSearchParams({ range: options.range ?? '24h' })
  options.serviceIds?.forEach((id) => query.append('serviceIds', String(id)))
  if (options.level) query.set('level', options.level)
  if (options.search?.trim()) query.set('search', options.search.trim())
  return query
}
