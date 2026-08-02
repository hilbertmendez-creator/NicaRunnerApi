import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from './auth-context'
import type { UserRole } from '../api/types'
import { LoadingText } from '@nicarunner/ui'

interface ProtectedRouteProps {
  allowedRoles?: UserRole[]
}

export function ProtectedRoute({ allowedRoles }: ProtectedRouteProps) {
  const { user, isAuthenticated, isLoading, logout } = useAuth()

  if (isLoading) {
    return (
      <div
        className="flex min-h-screen items-center justify-center"
        style={{ background: 'var(--bg-app)' }}
      >
        <LoadingText message="Comprobando sesión…" />
      </div>
    )
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  if (allowedRoles && user && !allowedRoles.includes(user.role)) {
    return (
      <div
        className="flex min-h-screen flex-col items-center justify-center gap-3 px-6 text-center"
        style={{ background: 'var(--bg-app)' }}
      >
        <p className="max-w-sm text-sm" style={{ color: 'var(--text-lo)' }}>
          Tu rol ({user.role}) no puede usar el back office. Entrá con la app móvil de captura,
          o pedí acceso de Administrador o Lector.
        </p>
        <button type="button" onClick={logout} className="text-sm hover:underline" style={{ color: 'var(--accent)' }}>
          Cerrar sesión
        </button>
      </div>
    )
  }

  return <Outlet />
}
