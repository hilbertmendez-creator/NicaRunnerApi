import { useState } from 'react'
import { Outlet, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/auth-context'
import { ThemeSwitcher } from './ThemeSwitcher'
import { TopbarRaceSelect } from './TopbarRaceSelect'
import { UserAccountMenu } from './UserAccountMenu'

interface NavItem {
  path: string
  label: string
  badge?: boolean
  icon: string
  adminOnly?: boolean
}

const NAV: Array<NavItem | 'sep'> = [
  {
    path: '/',
    label: 'Dashboard',
    icon: '<rect x="1" y="1" width="6" height="6" rx="1.5"/><rect x="9" y="1" width="6" height="6" rx="1.5"/><rect x="1" y="9" width="6" height="6" rx="1.5"/><rect x="9" y="9" width="6" height="6" rx="1.5"/>',
  },
  {
    path: '/carreras',
    label: 'Carreras',
    icon: '<path d="M3 3h10M3 6h7M3 9h8M3 12h5"/><circle cx="12.5" cy="10.5" r="2.5"/><path d="M12.5 8.5V7"/>',
  },
  {
    path: '/resultados',
    label: 'Resultados',
    icon: '<path d="M2 4h12M2 8h8M2 12h10"/>',
  },
  {
    path: '/resultados',
    label: 'Controversias',
    badge: true,
    icon: '<path d="M8 2L9.5 6h4.5l-3.6 2.6 1.4 4.4L8 10.4 4.2 13 5.6 8.6 2 6h4.5z"/>',
  },
  'sep',
  {
    path: '/usuarios',
    label: 'Usuarios',
    adminOnly: true,
    icon: '<path d="M2 11a4 4 0 018 0"/><circle cx="6" cy="5" r="2.5"/><path d="M11 7l1.5 1.5L15 6"/>',
  },
  {
    path: '/categorias',
    label: 'Categorías',
    adminOnly: true,
    icon: '<path d="M2 4h12M2 8h12M2 12h12"/><circle cx="5" cy="4" r="1.5" fill="currentColor" stroke="none"/><circle cx="9" cy="8" r="1.5" fill="currentColor" stroke="none"/><circle cx="7" cy="12" r="1.5" fill="currentColor" stroke="none"/>',
  },
]

const PAGE_TITLES: Record<string, string> = {
  '/': 'Dashboard',
  '/carreras': 'Carreras',
  '/resultados': 'Resultados',
  '/notificaciones': 'Notificaciones',
  '/usuarios': 'Usuarios',
  '/categorias': 'Categorías',
  '/enlaces': 'Enlaces',
}

export function AppLayout() {
  const { user } = useAuth()
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const location = useLocation()
  const navigate = useNavigate()

  const pageTitle = PAGE_TITLES[location.pathname] ?? 'NicaRunner'

  const visibleNav = NAV.filter((item) => {
    if (item === 'sep') return true
    if (item.adminOnly) return user?.role === 'Administrador'
    return true
  })

  return (
    <div style={{ display: 'flex', height: '100vh', overflow: 'hidden', background: 'var(--bg-app)' }}>
      {sidebarOpen && (
        <div
          onClick={() => setSidebarOpen(false)}
          aria-hidden="true"
          style={{
            position: 'fixed',
            inset: 0,
            background: 'rgba(0,0,0,.5)',
            zIndex: 30,
          }}
        />
      )}

      <aside
        className={`nr-sidebar ${sidebarOpen ? 'open' : 'closed'}`}
        style={{
          width: 52,
          flexShrink: 0,
          background: 'var(--bg-sb)',
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          padding: '12px 0',
          gap: 2,
          borderRight: '1px solid rgba(255,255,255,.06)',
          zIndex: 20,
        }}
      >
        <div
          style={{
            width: 32,
            height: 32,
            borderRadius: 7,
            background: 'var(--ac)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            marginBottom: 14,
            flexShrink: 0,
          }}
        >
          <svg
            width="16"
            height="16"
            viewBox="0 0 16 16"
            fill="none"
            stroke="#fff"
            strokeWidth="1.8"
            strokeLinecap="round"
            strokeLinejoin="round"
          >
            <path d="M2 13L6 3l3 6 2-3 3 7" />
          </svg>
        </div>

        <nav
          aria-label="Navegación principal"
          style={{
            display: 'flex',
            flexDirection: 'column',
            gap: 2,
            flex: 1,
            width: '100%',
            padding: '0 8px',
          }}
        >
          {visibleNav.map((item, i) => {
            if (item === 'sep') {
              return (
                <div
                  key={`sep-${i}`}
                  style={{
                    width: 24,
                    height: 1,
                    background: 'rgba(255,255,255,.07)',
                    margin: '6px auto',
                  }}
                />
              )
            }

            const isActive =
              item.path === '/'
                ? location.pathname === '/'
                : item.label === 'Controversias'
                  ? false
                  : location.pathname.startsWith(item.path)

            return (
              <div key={`${item.path}-${item.label}`} style={{ margin: '0 auto' }}>
                <button
                  type="button"
                  aria-current={isActive ? 'page' : undefined}
                  aria-label={item.label}
                  onClick={() => {
                    navigate(item.path)
                    setSidebarOpen(false)
                  }}
                  style={{
                    width: 36,
                    height: 36,
                    borderRadius: 7,
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    border: 'none',
                    cursor: 'pointer',
                    background: isActive ? 'var(--ac-bg)' : 'transparent',
                    color: isActive ? 'var(--ac)' : 'rgba(255,255,255,.45)',
                    transition: 'background .12s',
                    position: 'relative',
                  }}
                  onMouseEnter={(e) => {
                    if (!isActive) (e.currentTarget as HTMLButtonElement).style.background = 'rgba(255,255,255,.07)'
                  }}
                  onMouseLeave={(e) => {
                    if (!isActive) (e.currentTarget as HTMLButtonElement).style.background = 'transparent'
                  }}
                >
                  <svg
                    width="16"
                    height="16"
                    viewBox="0 0 16 16"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="1.6"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    dangerouslySetInnerHTML={{ __html: item.icon }}
                  />
                  {item.badge && (
                    <span
                      aria-hidden="true"
                      style={{
                        position: 'absolute',
                        top: -2,
                        right: -2,
                        width: 8,
                        height: 8,
                        borderRadius: '50%',
                        background: '#EF4444',
                        border: '1.5px solid var(--bg-sb)',
                      }}
                    />
                  )}
                  <span
                    className="sb-tooltip"
                    aria-hidden="true"
                    style={{
                      position: 'absolute',
                      left: 'calc(100% + 8px)',
                      top: '50%',
                      transform: 'translateY(-50%)',
                      background: '#1E293B',
                      color: '#E2E8F0',
                      fontSize: 11,
                      fontWeight: 500,
                      padding: '4px 8px',
                      borderRadius: 5,
                      whiteSpace: 'nowrap',
                      pointerEvents: 'none',
                      zIndex: 100,
                      fontFamily: 'Inter, system-ui',
                    }}
                  >
                    {item.label}
                  </span>
                </button>
              </div>
            )
          })}
        </nav>

        <UserAccountMenu />
      </aside>

      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
        <header
          style={{
            height: 52,
            background: 'var(--bg-tb)',
            borderBottom: '1px solid var(--bd)',
            display: 'flex',
            alignItems: 'center',
            padding: '0 20px',
            gap: 12,
            flexShrink: 0,
          }}
        >
          <button
            type="button"
            onClick={() => setSidebarOpen(true)}
            aria-label="Abrir menú"
            className="mobile-menu-btn"
            style={{
              display: 'none',
              width: 30,
              height: 30,
              borderRadius: 6,
              alignItems: 'center',
              justifyContent: 'center',
              border: 'none',
              background: 'transparent',
              cursor: 'pointer',
              color: 'var(--tx-md)',
            }}
          >
            <svg width="15" height="15" viewBox="0 0 15 15" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round">
              <path d="M2 4h11M2 7.5h11M2 11h11" />
            </svg>
          </button>

          <span style={{ fontSize: 13, fontWeight: 600, color: 'var(--tx-hi)', fontFamily: 'Inter, system-ui' }}>
            {pageTitle}
          </span>

          <TopbarRaceSelect />

          <div style={{ marginLeft: 'auto', display: 'flex', alignItems: 'center', gap: 8 }}>
            <ThemeSwitcher />
            <div style={{ height: 18, width: 1, background: 'var(--bd)', margin: '0 4px' }} />
            <button
              type="button"
              aria-label="Notificaciones"
              style={{
                width: 30,
                height: 30,
                borderRadius: 6,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                border: 'none',
                background: 'transparent',
                cursor: 'pointer',
                color: 'var(--tx-md)',
                position: 'relative',
              }}
            >
              <svg width="15" height="15" viewBox="0 0 15 15" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round">
                <path d="M7.5 2a1 1 0 00-1 1V3.5A4 4 0 004.5 7c0 2 0 3-1 4h8c-1-1-1-2-1-4a4 4 0 00-2-3.5V3a1 1 0 00-1-1zM6 11a1.5 1.5 0 003 0" />
              </svg>
              <span
                aria-hidden="true"
                style={{
                  position: 'absolute',
                  top: 5,
                  right: 5,
                  width: 6,
                  height: 6,
                  borderRadius: '50%',
                  background: '#EF4444',
                  border: '1.5px solid var(--bg-tb)',
                }}
              />
            </button>
          </div>
        </header>

        <main style={{ flex: 1, overflowY: 'auto', padding: 20 }}>
          <Outlet />
        </main>
      </div>
    </div>
  )
}
