import { useCallback, useEffect, useState } from 'react'
import { Outlet, useLocation, useNavigate } from 'react-router-dom'
import { Toaster } from 'react-hot-toast'
import { getControversiasOpenCount } from '../api/endpoints'
import { useAuth } from '../auth/auth-context'
import { CONTROVERSIAS_CHANGED_EVENT } from '../features/controversias/ControversiasPage'
import { useActiveRace } from '../hooks/useActiveRace'
import { NicaRunnerLogo } from '../routes/NicaRunnerLogo'
import { AdminNotificationsMenu } from './AdminNotificationsMenu'
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
    path: '/controversias',
    label: 'Controversias',
    icon: '<path d="M8 2L9.5 6h4.5l-3.6 2.6 1.4 4.4L8 10.4 4.2 13 5.6 8.6 2 6h4.5z"/>',
  },
  {
    path: '/notificaciones',
    label: 'Notificaciones',
    icon: '<path d="M8 1.5a1 1 0 00-1 1V3A4 4 0 004 6.5c0 2 0 3-1 4h10c-1-1-1-2-1-4A4 4 0 009 3V2.5a1 1 0 00-1-1zM6.5 11.5a1.5 1.5 0 003 0"/>',
  },
  {
    path: '/enlaces',
    label: 'Enlaces',
    icon: '<path d="M6.5 9.5a3 3 0 010-4.2l1.4-1.4a3 3 0 014.2 4.2l-.7.7"/><path d="M9.5 6.5a3 3 0 010 4.2l-1.4 1.4a3 3 0 01-4.2-4.2l.7-.7"/>',
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
  {
    path: '/dorsales-en-disputa',
    label: 'Dorsales en disputa',
    adminOnly: true,
    icon: '<rect x="2" y="2" width="12" height="12" rx="2"/><path d="M5.5 6h5M5.5 10h3"/>',
  },
]

const PAGE_TITLES: Record<string, string> = {
  '/': 'Dashboard',
  '/carreras': 'Carreras',
  '/resultados': 'Resultados',
  '/controversias': 'Controversias',
  '/notificaciones': 'Notificaciones',
  '/usuarios': 'Usuarios',
  '/categorias': 'Categorías',
  '/enlaces': 'Enlaces',
  '/dorsales-en-disputa': 'Dorsales en disputa',
}

const SIDEBAR_KEY = 'nicarunner-sidebar-expanded'
// Tailwind 'xl' breakpoint: a partir de acá el rail colapsado deja de ganar espacio
// útil, así que arranca desplegado salvo que el usuario ya haya elegido lo contrario.
const LARGE_SCREEN_BREAKPOINT = 1280

export function AppLayout() {
  const { user } = useAuth()
  const { raceId } = useActiveRace()
  const [expanded, setExpanded] = useState(() => {
    try {
      const stored = localStorage.getItem(SIDEBAR_KEY)
      if (stored === 'true') return true
      if (stored === 'false') return false
      return window.innerWidth >= LARGE_SCREEN_BREAKPOINT
    } catch {
      return false
    }
  })
  const [mobileOpen, setMobileOpen] = useState(false) // ephemeral drawer state, never persisted
  const [openDisputeCount, setOpenDisputeCount] = useState(0)
  const location = useLocation()
  const navigate = useNavigate()

  const pageTitle = PAGE_TITLES[location.pathname] ?? 'NicaRunner'

  // Badge honesto: solo si open-count > 0 para la carrera activa.
  const refreshOpenCount = useCallback(() => {
    if (!raceId) {
      setOpenDisputeCount(0)
      return
    }
    getControversiasOpenCount(raceId)
      .then((dto) => setOpenDisputeCount(dto.count))
      .catch(() => setOpenDisputeCount(0))
  }, [raceId])

  useEffect(() => {
    refreshOpenCount()
    window.addEventListener(CONTROVERSIAS_CHANGED_EVENT, refreshOpenCount)
    return () => window.removeEventListener(CONTROVERSIAS_CHANGED_EVENT, refreshOpenCount)
  }, [refreshOpenCount])

  useEffect(() => {
    try {
      localStorage.setItem(SIDEBAR_KEY, expanded ? 'true' : 'false')
    } catch {
      /* modo privado: nada que persistir */
    }
  }, [expanded])

  useEffect(() => {
    if (!mobileOpen) return
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setMobileOpen(false)
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [mobileOpen])

  const visibleNav = NAV.filter((item) => {
    if (item === 'sep') return true
    if (item.adminOnly) return user?.role === 'Administrador'
    return true
  })

  return (
    <div style={{ display: 'flex', height: '100vh', overflow: 'hidden', background: 'var(--bg-app)' }}>
      <a href="#main-content" className="skip-link">
        Saltar al contenido
      </a>
      <div
        className={`nr-scrim ${mobileOpen ? 'visible' : ''}`}
        aria-hidden="true"
        onClick={() => setMobileOpen(false)}
      />

      <aside
        className={`nr-sidebar ${mobileOpen ? 'open' : 'closed'} ${expanded ? 'expanded' : ''}`}
        style={{
          flexShrink: 0,
          background: 'var(--bg-sb)',
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          padding: '12px 0',
          gap: 2,
          borderRight: '1px solid var(--sb-border)',
          zIndex: 20,
        }}
      >
        <button
          type="button"
          className="nr-sb-toggle"
          aria-expanded={expanded}
          aria-label={expanded ? 'Contraer menú' : 'Expandir menú'}
          onClick={() => setExpanded((e) => !e)}
          style={{
            width: 36,
            height: 36,
            borderRadius: 7,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            border: 'none',
            background: 'transparent',
            cursor: 'pointer',
            color: 'var(--sb-muted)',
            marginBottom: 8,
            flexShrink: 0,
          }}
        >
          <svg width="15" height="15" viewBox="0 0 15 15" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round">
            <path d="M2 4h11M2 7.5h11M2 11h11" />
          </svg>
        </button>

        <div
          style={{
            width: 32,
            height: 32,
            borderRadius: 7,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            marginBottom: 14,
            flexShrink: 0,
          }}
        >
          <NicaRunnerLogo className="h-6 w-6" style={{ color: 'var(--ac)' }} />
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
                    background: 'var(--sb-sep)',
                    margin: '6px auto',
                  }}
                />
              )
            }

            // Controversias usa ruta propia; isActive estándar (sin forzar false).
            const isActive =
              item.path === '/'
                ? location.pathname === '/'
                : location.pathname.startsWith(item.path)
            const showBadge =
              item.path === '/controversias' ? openDisputeCount > 0 : Boolean(item.badge)

            return (
              <div key={`${item.path}-${item.label}`} className="nr-nav-wrap" style={{ margin: '0 auto' }}>
                <button
                  type="button"
                  className="nr-nav-btn"
                  aria-current={isActive ? 'page' : undefined}
                  aria-label={
                    item.path === '/controversias' && openDisputeCount > 0
                      ? `${item.label} (${openDisputeCount} abiertas)`
                      : item.label
                  }
                  onClick={() => {
                    navigate(item.path)
                    setMobileOpen(false)
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
                    background: isActive ? 'var(--sb-active-bg)' : 'transparent',
                    color: isActive ? 'var(--sb-text)' : 'var(--sb-muted)',
                    transition: 'background .12s',
                    position: 'relative',
                  }}
                  onMouseEnter={(e) => {
                    if (!isActive) (e.currentTarget as HTMLButtonElement).style.background = 'var(--sb-hover)'
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
                  <span className="nav-label">{item.label}</span>
                  {showBadge && (
                    <span
                      aria-hidden="true"
                      style={{
                        position: 'absolute',
                        top: -2,
                        right: -2,
                        width: 8,
                        height: 8,
                        borderRadius: '50%',
                        background: 'var(--er-tx, #EF4444)',
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
                      background: 'var(--bg-sb)',
                      color: 'var(--sb-fg)',
                      fontSize: 11,
                      fontWeight: 500,
                      padding: '4px 8px',
                      borderRadius: 5,
                      whiteSpace: 'nowrap',
                      pointerEvents: 'none',
                      zIndex: 100,
                      fontFamily: 'Inter, system-ui',
                      border: '1px solid var(--sb-sep)',
                      boxShadow: 'var(--shadow-md)',
                    }}
                  >
                    {item.label}
                  </span>
                </button>
              </div>
            )
          })}
        </nav>
      </aside>

      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
        <header
          className="nr-topbar"
          style={{
            height: 56,
            background: 'var(--bg-tb)',
            borderBottom: '1px solid var(--bd)',
            display: 'flex',
            alignItems: 'center',
            padding: '0 24px',
            gap: 12,
            flexShrink: 0,
          }}
        >
          <button
            type="button"
            onClick={() => setMobileOpen(true)}
            aria-label="Abrir menú"
            className="mobile-menu-btn nr-icon-btn"
            style={{
              display: 'none',
              width: 32,
              height: 32,
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

          <span
            style={{
              fontSize: 13,
              fontWeight: 600,
              color: 'var(--tx-hi)',
              fontFamily: 'Inter, system-ui',
              minWidth: 0,
              flex: '0 1 auto',
              maxWidth: '40%',
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              whiteSpace: 'nowrap',
            }}
            title={pageTitle}
          >
            {pageTitle}
          </span>

          <TopbarRaceSelect />

          <div style={{ marginLeft: 'auto', display: 'flex', alignItems: 'center', gap: 8 }}>
            <ThemeSwitcher />
            <UserAccountMenu />
            <div style={{ height: 18, width: 1, background: 'var(--bd)', margin: '0 4px' }} />
            <AdminNotificationsMenu />
          </div>
        </header>

        <main id="main-content" tabIndex={-1} className="nr-main" style={{ flex: 1, overflowY: 'auto', padding: 24 }}>
          <Outlet />
        </main>
      </div>
      <Toaster position="top-right" />
    </div>
  )
}
