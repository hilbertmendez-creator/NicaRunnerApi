import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/auth-context'
import { Button } from '@nicarunner/ui'

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
    <div className="min-h-screen bg-gray-100">
      <header className="border-b border-gray-200 bg-white px-6 py-2.5">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-6">
            <div className="flex items-center gap-2">
              <span className="text-base font-semibold text-orange-700">nicaRunner</span>
            </div>
            <nav className="flex items-center gap-1">
              {NAV_ITEMS.map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  end={item.to === '/'}
                  className={({ isActive }) =>
                    `rounded-md px-3 py-1.5 text-sm font-medium ${
                      isActive ? 'bg-gray-100 text-gray-900' : 'text-gray-500 hover:text-gray-800'
                    }`
                  }
                >
                  {item.label}
                </NavLink>
              ))}
            </nav>
          </div>
          <div className="flex items-center gap-3">
            <span className="text-sm text-gray-500">{user?.nombre}</span>
            <div className="flex h-7 w-7 items-center justify-center rounded-full bg-orange-100 text-xs font-medium text-orange-800">
              {initials}
            </div>
            <Button onClick={logout}>Salir</Button>
          </div>
        </div>
      </header>
      <main className="p-6">
        <Outlet />
      </main>
    </div>
  )
}
