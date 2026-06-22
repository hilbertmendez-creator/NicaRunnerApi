import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/auth-context'

const NAV_ITEMS = [
  { to: '/', label: 'Dashboard' },
  { to: '/carreras', label: 'Carreras' },
  { to: '/resultados', label: 'Resultados' },
  { to: '/notificaciones', label: 'Notificaciones' },
  { to: '/enlaces', label: 'Enlaces públicos' },
] as const

export function AppLayout() {
  const { user, logout } = useAuth()
  const initials = (user?.nombre ?? '?')
    .split(' ')
    .map((part) => part[0])
    .slice(0, 2)
    .join('')
    .toUpperCase()

  return (
    <div className="min-h-screen bg-zinc-50">
      <header className="border-b border-slate-blue-800 bg-slate-blue-900 px-6 py-2.5">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-6">
            <div className="flex items-center gap-2">
              <span className="text-base font-semibold text-amber-400">nicaRunner</span>
            </div>
            <nav className="flex items-center gap-1">
              {NAV_ITEMS.map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  end={item.to === '/'}
                  className={({ isActive }) =>
                    `px-3 py-1.5 text-sm font-medium ${
                      isActive ? 'bg-slate-blue-800 text-white' : 'text-zinc-400 hover:text-white'
                    }`
                  }
                >
                  {item.label}
                </NavLink>
              ))}
            </nav>
          </div>
          <div className="flex items-center gap-3">
            <span className="text-sm text-zinc-400">{user?.nombre}</span>
            <div className="flex h-7 w-7 items-center justify-center border border-slate-blue-700 bg-slate-blue-800 text-xs font-medium text-zinc-200">
              {initials}
            </div>
            <button
              type="button"
              onClick={logout}
              className="h-8 border border-slate-blue-700 px-3 text-sm font-medium text-zinc-300 hover:border-zinc-400 hover:text-white"
            >
              Salir
            </button>
          </div>
        </div>
      </header>
      <main className="p-6">
        <Outlet />
      </main>
    </div>
  )
}
