export class ApiError extends Error {
  readonly status: number

  constructor(
    status: number,
    message: string,
  ) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

type RequestOptions = Omit<RequestInit, 'body' | 'method'> & {
  body?: unknown
}

class BaseApiService {
  get<T>(path: string, options?: RequestOptions) {
    return this.request<T>(path, { ...options, method: 'GET' })
  }

  post<T>(path: string, body?: unknown, options?: RequestOptions) {
    return this.request<T>(path, { ...options, method: 'POST', body })
  }

  patch<T>(path: string, body?: unknown, options?: RequestOptions) {
    return this.request<T>(path, { ...options, method: 'PATCH', body })
  }

  delete<T>(path: string, options?: RequestOptions) {
    return this.request<T>(path, { ...options, method: 'DELETE' })
  }

  private async request<T>(path: string, options: RequestOptions & { method: string }) {
    const headers = new Headers(options.headers)
    const hasBody = options.body !== undefined

    if (hasBody && !headers.has('Content-Type')) {
      headers.set('Content-Type', 'application/json')
    }

    const response = await fetch(path, {
      ...options,
      headers,
      body: hasBody ? JSON.stringify(options.body) : undefined,
    })

    if (!response.ok) {
      throw new ApiError(
        response.status,
        `API request failed with status ${response.status}`,
      )
    }

    if (response.status === 204) return undefined as T

    return response.json() as Promise<T>
  }
}

export const apiService = new BaseApiService()
