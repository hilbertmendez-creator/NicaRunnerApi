import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

export function AppLayout() {
  const { user, logout } = useAuth()

  const linkClass = ({ isActive }: { isActive: boolean }) =>
    `rounded px-3 py-2 text-sm font-medium ${
      isActive ? 'bg-blue-600 text-white' : 'text-gray-700 hover:bg-gray-200'
    }`

  return (
    <div className="min-h-screen bg-gray-100">
      <header className="flex items-center justify-between bg-white px-6 py-3 shadow-sm">
        <div className="flex items-center gap-2">
          <span className="text-lg font-semibold text-gray-900">nicaRunner</span>
          <span className="text-sm text-gray-400">Back Office</span>
        </div>
        <nav className="flex gap-2">
          <NavLink to="/" className={linkClass} end>
            Dashboard
          </NavLink>
          <NavLink to="/resultados" className={linkClass}>
            Resultados
          </NavLink>
        </nav>
        <div className="flex items-center gap-3">
          <span className="text-sm text-gray-600">
            {user?.nombre} <span className="text-gray-400">({user?.role})</span>
          </span>
          <button
            onClick={logout}
            className="rounded border border-gray-300 px-3 py-1.5 text-sm text-gray-700 hover:bg-gray-200"
          >
            Salir
          </button>
        </div>
      </header>
      <main className="p-6">
        <Outlet />
      </main>
    </div>
  )
}
