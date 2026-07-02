import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/auth-context'
import { ThemeSwitcher } from './ThemeSwitcher'
import logoEmblem from '../assets/logo-emblem.png'

interface NavItem {
  to: string
  label: string
  icon: string
  end?: boolean
}

const NAV_GROUPS: { group: string; items: NavItem[] }[] = [
  {
    group: 'Inicio',
    items: [
      { to: '/', label: 'Dashboard', icon: '▦', end: true },
      { to: '/carreras', label: 'Carreras', icon: '★' },
    ],
  },
  {
    group: 'Datos',
    items: [{ to: '/resultados', label: 'Resultados', icon: '▦' }],
  },
  {
    group: 'Reportes',
    items: [
      { to: '/notificaciones', label: 'Notificaciones', icon: '🔔' },
      { to: '/enlaces', label: 'Enlaces públicos', icon: '↗' },
    ],
  },
]

const ADMIN_ONLY_NAV_ITEMS = [{ to: '/usuarios', label: 'Usuarios' }] as const

export function AppLayout() {
  const { user, logout } = useAuth()
  const initials = (user?.nombre ?? '?')
    .split(' ')
    .map((part) => part[0])
    .slice(0, 2)
    .join('')
    .toUpperCase()

  return (
    <div style={{ display: 'flex', height: '100vh', overflow: 'hidden', background: 'var(--bg-app)' }}>
      {/* ── Sidebar ── */}
      <aside
        className="sidebar-inner"
        style={{
          width: 220,
          flexShrink: 0,
          display: 'flex',
          flexDirection: 'column',
          borderRight: '1px solid var(--bd)',
          background: 'var(--bg-sidebar)',
        }}
      >
        {/* Logo */}
        <div style={{ padding: '18px 16px 14px', borderBottom: '1px solid var(--bd)' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <img src={logoEmblem} alt="NicaRunner" style={{ width: 24, height: 24, borderRadius: 5 }} />
            <span style={{ font: '700 13px Inter', color: 'var(--sb-text)' }}>NicaRunner</span>
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 6, marginTop: 8 }}>
            <span className="dot-live" />
            <span style={{ font: '400 10px Inter', color: 'var(--sb-muted)' }}>Sistema en línea</span>
          </div>
        </div>

        {/* Nav */}
        <nav style={{ padding: 8, flex: 1, overflowY: 'auto' }}>
          {NAV_GROUPS.map((grp) => (
            <div key={grp.group}>
              <div
                style={{
                  padding: '5px 8px',
                  font: '500 9px Inter',
                  color: 'var(--sb-muted)',
                  letterSpacing: '.7px',
                  textTransform: 'uppercase',
                  margin: '8px 0 2px',
                }}
              >
                {grp.group}
              </div>
              {grp.items.map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  end={item.end}
                  style={({ isActive }) => ({
                    display: 'flex',
                    alignItems: 'center',
                    gap: 9,
                    padding: isActive ? '6px 9px 6px 7px' : '7px 9px',
                    borderRadius: 6,
                    borderLeft: isActive ? '2.5px solid var(--sb-active-bd)' : '2.5px solid transparent',
                    background: isActive ? 'var(--sb-active-bg)' : 'transparent',
                    textDecoration: 'none',
                    marginBottom: 1,
                    color: isActive ? 'var(--sb-text)' : 'var(--sb-muted)',
                    font: `${isActive ? 600 : 500} 12.5px Inter`,
                  })}
                >
                  <span style={{ width: 16, textAlign: 'center' }}>{item.icon}</span>
                  <span style={{ flex: 1 }}>{item.label}</span>
                </NavLink>
              ))}
            </div>
          ))}
        </nav>

        {/* User + logout */}
        <div style={{ padding: 12, borderTop: '1px solid var(--bd)', display: 'flex', alignItems: 'center', gap: 9 }}>
          <div
            style={{
              width: 28,
              height: 28,
              borderRadius: '50%',
              background: 'var(--accent-bg)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              flexShrink: 0,
            }}
          >
            <span style={{ font: '600 10px Inter', color: 'var(--accent)' }}>{initials}</span>
          </div>
          <div style={{ flex: 1, minWidth: 0 }}>
            <div
              style={{
                font: '500 11.5px Inter',
                color: 'var(--sb-text)',
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap',
              }}
            >
              {user?.nombre}
            </div>
            <div style={{ font: '400 10px Inter', color: 'var(--sb-muted)' }}>{user?.role ?? 'Usuario'}</div>
          </div>
          <button
            type="button"
            onClick={logout}
            title="Salir"
            style={{
              padding: '4px 8px',
              background: 'transparent',
              border: '1px solid var(--gh-bd)',
              borderRadius: 'var(--radius-btn)',
              font: '400 10.5px Inter',
              color: 'var(--gh-text)',
              cursor: 'pointer',
            }}
          >
            Salir
          </button>
        </div>
      </aside>

      {/* ── Right column ── */}
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
        {/* Topbar */}
        <div
          style={{
            height: 56,
            background: 'var(--bg-topbar)',
            borderBottom: '1px solid var(--bd)',
            display: 'flex',
            alignItems: 'center',
            padding: '0 22px',
            gap: 12,
            flexShrink: 0,
          }}
        >
          <div style={{ flex: 1, minWidth: 0 }}>
            <div style={{ font: '600 14px Inter', color: 'var(--text-hi)' }}>NicaRunner Backoffice</div>
            <div style={{ font: '400 10.5px Inter', color: 'var(--text-xs)', marginTop: 1 }}>
              Gestión de competencias de atletismo
            </div>
          </div>
          <ThemeSwitcher />
        </div>

        {/* Content */}
        <main style={{ flex: 1, overflow: 'auto', padding: 24 }}>
          <Outlet />
        </main>
      </div>
    </div>
  )
}
