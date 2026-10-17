import { createContext } from 'react'
import type { LoginRequest, RegisterRequest } from '../services/auth-api'
import type { AuthUser } from '../types/api'

export interface AuthContextValue {
  user: AuthUser | null
  isLoading: boolean
  login: (request: LoginRequest) => Promise<void>
  register: (request: RegisterRequest) => Promise<void>
  logout: () => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | null>(null)
