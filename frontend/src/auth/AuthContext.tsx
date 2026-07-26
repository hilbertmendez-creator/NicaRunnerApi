import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { login as loginRequest, getCurrentUser } from '../api/endpoints'
import { apiClient, setUnauthorizedHandler } from '../api/client'
import { AuthContext, type AuthContextValue, type CurrentUser } from './auth-context'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<CurrentUser | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  const logout = useCallback(() => {
    // Best-effort: revoke el refresh token y limpia las cookies httpOnly en
    // el server. No await — la sesión local se limpia igual sin importar el
    // resultado de red.
    apiClient.post('/auth/logout').catch(() => {})
    setUser(null)
  }, [])

  useEffect(() => {
    setUnauthorizedHandler(() => setUser(null))
  }, [])

  // Al montar, la app no tiene forma de leer la cookie httpOnly con JS —
  // le pregunta al server si hay sesión activa.
  useEffect(() => {
    let cancelled = false
    getCurrentUser()
      .then((current) => {
        if (cancelled) return
        setUser(current)
      })
      .catch(() => {
        // Sin sesión (401) u otro error de red — se trata igual como "no logueado".
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [])

  const login = useCallback(async (email: string, password: string) => {
    const response = await loginRequest(email, password)
    setUser({
      userId: response.userId,
      email: response.email,
      nombre: response.nombre,
      role: response.role,
      mustChangePassword: response.mustChangePassword,
    })
  }, [])

  const clearMustChangePassword = useCallback(() => {
    setUser((current) => (current ? { ...current, mustChangePassword: false } : current))
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({ user, isAuthenticated: user !== null, isLoading, login, logout, clearMustChangePassword }),
    [user, isLoading, login, logout, clearMustChangePassword],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
