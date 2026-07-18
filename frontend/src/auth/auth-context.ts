import { createContext, useContext } from 'react'
import type { UserRole } from '../api/types'

export interface CurrentUser {
  userId: number
  email: string
  nombre: string
  role: UserRole
  mustChangePassword: boolean
}

export interface AuthContextValue {
  user: CurrentUser | null
  isAuthenticated: boolean
  // true mientras se resuelve la sesión inicial contra GET /auth/me (el
  // frontend no puede leer la cookie httpOnly, así que le pregunta al
  // server). ProtectedRoute espera a que esto termine antes de decidir si
  // redirige a /login.
  isLoading: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => void
  clearMustChangePassword: () => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth debe usarse dentro de AuthProvider')
  return ctx
}
