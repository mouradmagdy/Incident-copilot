import type { AuthUser } from '../types/api'
import { apiService } from './base-api-service'

export interface LoginRequest {
  email: string
  password: string
}

export interface RegisterRequest extends LoginRequest {
  displayName: string
}

export const authApi = {
  me(signal?: AbortSignal) {
    return apiService.get<AuthUser>('/api/auth/me', { signal })
  },

  login(request: LoginRequest) {
    return apiService.post<AuthUser>('/api/auth/login', request)
  },

  register(request: RegisterRequest) {
    return apiService.post<AuthUser>('/api/auth/register', request)
  },

  logout() {
    return apiService.post<void>('/api/auth/logout')
  },
}
