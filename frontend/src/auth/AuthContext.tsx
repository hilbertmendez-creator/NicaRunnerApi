import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { login as loginRequest, logoutRequest } from '../api/endpoints'
import {
  getStoredRefreshToken,
  getStoredToken,
  setStoredRefreshToken,
  setStoredToken,
  setUnauthorizedHandler,
} from '../api/client'
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

  const clearLocalSession = useCallback(() => {
    setStoredToken(null)
    setStoredRefreshToken(null)
    localStorage.removeItem(USER_STORAGE_KEY)
    setUser(null)
  }, [])

  // Logout "normal": intenta revocar la familia de refresh tokens en el
  // backend, pero SIEMPRE limpia el estado local. Un backend caído o un token
  // ya inválido no bloquean al usuario de salir del back office.
  const logout = useCallback(async () => {
    const refreshToken = getStoredRefreshToken()
    if (refreshToken) {
      try {
        await logoutRequest(refreshToken)
      } catch {
        // Best-effort — si el backend no responde igual limpiamos local
      }
    }
    clearLocalSession()
  }, [clearLocalSession])

  // Hard logout: llamado por el interceptor cuando el /refresh falla (token
  // expirado o familia revocada por replay detection). NO hace request al
  // backend — el refresh token que tenemos ya no vale.
  useEffect(() => {
    setUnauthorizedHandler(clearLocalSession)
  }, [clearLocalSession])

  const login = useCallback(async (email: string, password: string) => {
    const response = await loginRequest(email, password)
    setStoredToken(response.token)
    setStoredRefreshToken(response.refreshToken)
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
