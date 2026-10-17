import { useQueryClient } from '@tanstack/react-query'
import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import { authApi, type LoginRequest, type RegisterRequest } from '../services/auth-api'
import { ApiError } from '../services/base-api-service'
import type { AuthUser } from '../types/api'
import { AuthContext } from './auth-context-state'

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const [user, setUser] = useState<AuthUser | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    const controller = new AbortController()

    void authApi.me(controller.signal)
      .then(setUser)
      .catch((error: unknown) => {
        if (!(error instanceof ApiError && error.status === 401)) {
          console.error('Could not restore the authenticated session.', error)
        }
        setUser(null)
      })
      .finally(() => setIsLoading(false))

    const clearSession = () => setUser(null)
    window.addEventListener('incident-copilot:unauthorized', clearSession)

    return () => {
      controller.abort()
      window.removeEventListener('incident-copilot:unauthorized', clearSession)
    }
  }, [])

  const login = useCallback(async (request: LoginRequest) => {
    setUser(await authApi.login(request))
  }, [])

  const register = useCallback(async (request: RegisterRequest) => {
    setUser(await authApi.register(request))
  }, [])

  const logout = useCallback(async () => {
    try {
      await authApi.logout()
    } catch {
      // Clearing the local session is still correct if the server session
      // already expired or the logout request could not reach the API.
    } finally {
      setUser(null)
      queryClient.clear()
    }
  }, [queryClient])

  const value = useMemo(() => ({
    user,
    isLoading,
    login,
    register,
    logout,
  }), [user, isLoading, login, register, logout])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
