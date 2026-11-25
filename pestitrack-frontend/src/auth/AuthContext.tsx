import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  type PropsWithChildren,
} from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { ApiError, apiRequest, registerUnauthorizedHandler } from '../lib/api'
import type { AuthResponse, LoginResponse, User } from '../types/api'

/* oxlint-disable react/only-export-components -- provider and hook form one public context API */

interface AuthContextValue {
  user: User | null
  isLoading: boolean
  isAdministrator: boolean
  isAuditor: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)
const authQueryKey = ['auth', 'me'] as const

async function loadCurrentUser(): Promise<User | null> {
  try {
    return (await apiRequest<AuthResponse>('/api/auth/me')).user
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) return null
    throw error
  }
}

export function AuthProvider({ children }: PropsWithChildren) {
  const queryClient = useQueryClient()
  const { data: user = null, isLoading } = useQuery({
    queryKey: authQueryKey,
    queryFn: loadCurrentUser,
    retry: false,
    staleTime: 60_000,
  })

  const login = useCallback(
    async (email: string, password: string) => {
      const response = await apiRequest<LoginResponse>('/api/auth/login', {
        method: 'POST',
        body: JSON.stringify({ email, password }),
      })
      queryClient.setQueryData(authQueryKey, response.user)
    },
    [queryClient],
  )

  const logout = useCallback(async () => {
    await apiRequest('/api/auth/logout', { method: 'POST' })
    queryClient.setQueryData(authQueryKey, null)
    queryClient.removeQueries({ queryKey: ['farms'] })
    queryClient.removeQueries({ queryKey: ['farm-permissions'] })
  }, [queryClient])

  useEffect(() => {
    registerUnauthorizedHandler(() => {
      queryClient.setQueryData(authQueryKey, null)
      queryClient.removeQueries({ queryKey: ['farms'] })
      queryClient.removeQueries({ queryKey: ['farm-permissions'] })
    })
    return () => registerUnauthorizedHandler(null)
  }, [queryClient])

  const value = useMemo(
    () => ({
      user,
      isLoading,
      isAdministrator: user?.systemRoles.includes('SystemAdministrator') ?? false,
      isAuditor: user?.systemRoles.includes('SystemAuditor') ?? false,
      login,
      logout,
    }),
    [user, isLoading, login, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used inside AuthProvider')
  return context
}
