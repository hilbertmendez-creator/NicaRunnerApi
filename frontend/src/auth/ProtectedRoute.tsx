import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from './auth-context'
import type { UserRole } from '../api/types'

interface ProtectedRouteProps {
  allowedRoles?: UserRole[]
}

export function ProtectedRoute({ allowedRoles }: ProtectedRouteProps) {
  const { user, isAuthenticated, logout } = useAuth()

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  if (allowedRoles && user && !allowedRoles.includes(user.role)) {
    return (
      <div
        className="flex min-h-screen flex-col items-center justify-center gap-3 text-center"
        style={{ background: 'var(--bg-app)' }}
      >
        <p className="text-sm" style={{ color: 'var(--text-lo)' }}>
          Tu rol ({user.role}) no tiene acceso al back office. Usa la app móvil de captura.
        </p>
        <button onClick={logout} className="text-sm hover:underline" style={{ color: 'var(--accent)' }}>
          Cerrar sesión
        </button>
      </div>
    )
  }

  return <Outlet />
}
