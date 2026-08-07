import { useEffect, useRef, useState } from 'react'
import { useAuth } from '../auth/auth-context'
import { useRace } from '../hooks/useRace'
import { ThemeSwitcher } from './ThemeSwitcher'
import { MenuIcon } from './icons/MenuIcon'
import { ChevronsLeftIcon } from './icons/ChevronsLeftIcon'
import { ChevronsRightIcon } from './icons/ChevronsRightIcon'
import { ChevronDownIcon } from './icons/ChevronDownIcon'
import { LogoutIcon } from './icons/LogoutIcon'

interface TopbarProps {
  collapsed: boolean
  onToggleCollapsed: () => void
  onOpenMobile: () => void
}

export function Topbar({ collapsed, onToggleCollapsed, onOpenMobile }: TopbarProps) {
  const { user, logout } = useAuth()
  const { selectedRace, loading } = useRace()
  const [menuOpen, setMenuOpen] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)

  // Cierra el user-menu con clic fuera (referencia sidebar.js)
  useEffect(() => {
    if (!menuOpen) return
    function onDocPointer(e: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) setMenuOpen(false)
    }
    document.addEventListener('mousedown', onDocPointer)
    return () => document.removeEventListener('mousedown', onDocPointer)
  }, [menuOpen])

  const initials = (user?.nombre ?? '?')
    .split(' ')
    .map((part) => part[0])
    .slice(0, 2)
    .join('')
    .toUpperCase()

  const raceName = selectedRace?.nombre ?? (loading ? 'Cargando carreras…' : 'Sin carrera seleccionada')

  return (
    <header className="sb-topbar">
      <button
        type="button"
        onClick={onOpenMobile}
        aria-label="Abrir menú"
        className="sb-icon-btn lg:hidden"
      >
        <MenuIcon size={18} />
      </button>

      <button
        type="button"
        onClick={onToggleCollapsed}
        aria-label={collapsed ? 'Expandir menú' : 'Colapsar menú'}
        className="sb-icon-btn hidden lg:flex"
      >
        {collapsed ? <ChevronsRightIcon size={18} /> : <ChevronsLeftIcon size={18} />}
      </button>

      <div style={{ flex: 1, minWidth: 0 }}>
        <div className="sb-topbar-title">NicaRunner Backoffice</div>
        <div className="sb-topbar-subtitle hidden sm:block">Gestión de competencias de atletismo</div>
      </div>

      <div className="race-select" title={raceName}>
        <span className="dot-live" />
        <span className="race-select-name">{raceName}</span>
      </div>

      <div className="user-menu" ref={menuRef}>
        <button
          type="button"
          onClick={() => setMenuOpen((open) => !open)}
          aria-haspopup="menu"
          aria-expanded={menuOpen}
          className={`user-menu-trigger${menuOpen ? ' open' : ''}`}
        >
          <div className="user-avatar-sm">{initials}</div>
          <span className="user-menu-name hidden sm:inline">{user?.nombre}</span>
          <ChevronDownIcon className="user-menu-chevron" />
        </button>

        {menuOpen && (
          <div className="user-menu-dropdown" role="menu">
            <span className="dropdown-name">{user?.nombre}</span>
            <span className="dropdown-role">{user?.role ?? 'Usuario'}</span>
            <div className="dropdown-sep" />
            <ThemeSwitcher />
            <div className="dropdown-sep" />
            <button type="button" className="dropdown-item logout" role="menuitem" onClick={logout}>
              <LogoutIcon />
              Cerrar sesión
            </button>
          </div>
        )}
      </div>
    </header>
  )
}