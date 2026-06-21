import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { login as loginRequest } from '../api/endpoints'
import { getStoredToken, setStoredToken, setUnauthorizedHandler } from '../api/client'
import type { UserRole } from '../api/types'

interface CurrentUser {
  userId: number
  email: string
  nombre: string
  role: UserRole
}

const USER_STORAGE_KEY = 'nicarunner.user'

interface AuthContextValue {
  user: CurrentUser | null
  isAuthenticated: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

function readStoredUser(): CurrentUser | null {
  const raw = localStorage.getItem(USER_STORAGE_KEY)
  if (!raw) return null
  try {
    return JSON.parse(raw) as CurrentUser
  } catch {
    return null
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<CurrentUser | null>(() =>
    getStoredToken() ? readStoredUser() : null,
  )

  const logout = useCallback(() => {
    setStoredToken(null)
    localStorage.removeItem(USER_STORAGE_KEY)
    setUser(null)
  }, [])

  useEffect(() => {
    setUnauthorizedHandler(logout)
  }, [logout])

  const login = useCallback(async (email: string, password: string) => {
    const response = await loginRequest(email, password)
    setStoredToken(response.token)
    const currentUser: CurrentUser = {
      userId: response.userId,
      email: response.email,
      nombre: response.nombre,
      role: response.role,
    }
    localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(currentUser))
    setUser(currentUser)
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({ user, isAuthenticated: user !== null, login, logout }),
    [user, login, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth debe usarse dentro de AuthProvider')
  return ctx
}
