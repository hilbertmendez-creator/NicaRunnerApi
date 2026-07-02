import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from './auth-context'

/**
 * Bloquea el resto del backoffice hasta que el usuario complete el cambio
 * de contraseña forzado (seed inicial o password temporal de un admin nuevo).
 */
export function RequirePasswordChanged() {
  const { user } = useAuth()

  if (user?.mustChangePassword) {
    return <Navigate to="/change-password" replace />
  }

  return <Outlet />
}
