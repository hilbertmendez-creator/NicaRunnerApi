import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { login as loginRequest } from '../api/endpoints'
import { getStoredToken, setStoredToken, setUnauthorizedHandler } from '../api/client'
import { AuthContext, type AuthContextValue, type CurrentUser } from './auth-context'

const USER_STORAGE_KEY = 'nicarunner.user'

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
      mustChangePassword: response.mustChangePassword,
    }
    localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(currentUser))
    setUser(currentUser)
  }, [])

  const clearMustChangePassword = useCallback(() => {
    setUser((current) => {
      if (!current) return current
      const updated = { ...current, mustChangePassword: false }
      localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(updated))
      return updated
    })
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({ user, isAuthenticated: user !== null, login, logout, clearMustChangePassword }),
    [user, login, logout, clearMustChangePassword],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
