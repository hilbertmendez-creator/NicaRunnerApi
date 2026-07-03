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
