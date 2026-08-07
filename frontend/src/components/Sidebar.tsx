import { NavLink } from 'react-router-dom'
import type { ComponentType } from 'react'
import { useAuth } from '../auth/auth-context'
import logoEmblem from '../assets/logo-emblem.png'
import { DashboardIcon } from './icons/DashboardIcon'
import { RacesIcon } from './icons/RacesIcon'
import { ResultsIcon } from './icons/ResultsIcon'
import { ControversiesIcon } from './icons/ControversiesIcon'
import { NotificationsIcon } from './icons/NotificationsIcon'
import { LinksIcon } from './icons/LinksIcon'
import { UsersIcon } from './icons/UsersIcon'
import { CategoriesIcon } from './icons/CategoriesIcon'
import type { IconProps } from './icons/types'

interface NavItem {
  to: string
  label: string
  Icon: ComponentType<IconProps>
  end?: boolean
  adminOnly?: boolean
  /** Número visible en la nav-badge. Slice 5 alimenta el conteo real (resumen). */
  badge?: number
}

const NAV_GROUPS: { group: string; items: NavItem[] }[] = [
  {
    group: 'Inicio',
    items: [
      { to: '/', label: 'Dashboard', Icon: DashboardIcon, end: true },
      { to: '/carreras', label: 'Carreras', Icon: RacesIcon },
    ],
  },
  {
    group: 'Datos',
    items: [{ to: '/resultados', label: 'Resultados', Icon: ResultsIcon }],
  },
  {
    group: 'Reportes',
    items: [
      { to: '/controversias', label: 'Controversias', Icon: ControversiesIcon, adminOnly: true, badge: 0 },
      { to: '/notificaciones', label: 'Notificaciones', Icon: NotificationsIcon },
      { to: '/enlaces', label: 'Enlaces públicos', Icon: LinksIcon },
    ],
  },
]

const ADMIN_NAV_ITEMS: NavItem[] = [
  { to: '/usuarios', label: 'Usuarios', Icon: UsersIcon },
  { to: '/categorias', label: 'Categorías', Icon: CategoriesIcon },
]

interface SidebarProps {
  mobileOpen: boolean
  onClose: () => void
}

export function Sidebar({ mobileOpen, onClose }: SidebarProps) {
  const { user } = useAuth()
  const isAdmin = user?.role === 'Administrador'

  const groups = NAV_GROUPS.map((grp) => ({
    ...grp,
    items: grp.items.filter((item) => !item.adminOnly || isAdmin),
  })).filter((grp) => grp.items.length > 0)

  return (
    <aside
      className={`sidebar-inner sb-rail fixed inset-y-0 left-0 z-40 lg:static lg:z-auto lg:translate-x-0 ${
        mobileOpen ? 'translate-x-0' : '-translate-x-full'
      }`}
    >
      <div className="sb-brand">
        <div className="sb-brand-row">
          <div className="sb-brand-main">
            <img src={logoEmblem} alt="NicaRunner" style={{ width: 24, height: 24, borderRadius: 5 }} />
            <span className="sb-brand-text">NicaRunner</span>
          </div>
          <button
            type="button"
            onClick={onClose}
            aria-label="Cerrar menú"
            className="sb-icon-btn lg:hidden"
          >
            ✕
          </button>
        </div>
        <div className="sb-brand-status">
          <span className="dot-live" />
          <span style={{ font: '400 10px Inter', color: 'var(--sb-muted)' }}>Sistema en línea</span>
        </div>
      </div>

      <nav className="sb-nav">
        {groups.map((grp) => (
          <div key={grp.group}>
            <div className="sb-group-label">{grp.group}</div>
            {grp.items.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.end}
                onClick={onClose}
                title={item.label}
                className={({ isActive }) => `sb-nav-item${isActive ? ' active' : ''}`}
              >
                <item.Icon size={16} />
                <span className="sb-nav-label">{item.label}</span>
                {typeof item.badge === 'number' && item.badge > 0 && (
                  <span className="sb-nav-badge">{item.badge}</span>
                )}
              </NavLink>
            ))}
          </div>
        ))}

        {isAdmin && (
          <div>
            <div className="sb-group-label">Administración</div>
            {ADMIN_NAV_ITEMS.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                onClick={onClose}
                title={item.label}
                className={({ isActive }) => `sb-nav-item${isActive ? ' active' : ''}`}
              >
                <item.Icon size={16} />
                <span className="sb-nav-label">{item.label}</span>
              </NavLink>
            ))}
          </div>
        )}
      </nav>
    </aside>
  )
}