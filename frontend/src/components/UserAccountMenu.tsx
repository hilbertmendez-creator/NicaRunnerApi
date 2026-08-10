import { useEffect, useRef, useState } from 'react'
import { useAuth } from '../auth/auth-context'

/** Trigger avatar+nombre+chevron en el topbar, con menú siempre montado (transiciona vía .open). Sin foto en API. */
export function UserAccountMenu() {
  const { user, logout } = useAuth()
  const [open, setOpen] = useState(false)
  const rootRef = useRef<HTMLDivElement>(null)

  const initials = (user?.nombre ?? '?')
    .split(' ')
    .map((p) => p[0])
    .slice(0, 2)
    .join('')
    .toUpperCase()

  useEffect(() => {
    if (!open) return
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setOpen(false)
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [open])

  // mousedown (no click) para que el cierre por fuera-de-foco llegue antes del click del trigger.
  useEffect(() => {
    if (!open) return
    const onDown = (e: MouseEvent) => {
      if (rootRef.current && !rootRef.current.contains(e.target as Node)) setOpen(false)
    }
    document.addEventListener('mousedown', onDown)
    return () => document.removeEventListener('mousedown', onDown)
  }, [open])

  return (
    <div ref={rootRef} className={`user-menu ${open ? 'open' : ''}`} style={{ position: 'relative' }}>
      <button
        type="button"
        className="nr-account-btn"
        title={user?.nombre ?? 'Mi cuenta'}
        aria-label="Menú de cuenta"
        aria-expanded={open}
        aria-haspopup="menu"
        onClick={() => setOpen((o) => !o)}
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: 8,
          height: 36,
          padding: '0 10px 0 4px',
          borderRadius: 20,
          background: 'transparent',
          cursor: 'pointer',
          border: open ? '1.5px solid var(--sb-sep)' : '1.5px solid transparent',
          fontFamily: 'Inter, system-ui',
        }}
      >
        <span
          className="user-menu-avatar"
          style={{
            background: 'var(--bg-hover)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            fontSize: 11,
            fontWeight: 600,
            color: 'var(--tx-hi)',
            flexShrink: 0,
          }}
        >
          {initials}
        </span>
        <span className="user-menu-name" style={{ fontSize: 13, fontWeight: 500, color: 'var(--tx-hi)' }}>
          {user?.nombre ?? 'Usuario'}
        </span>
        <svg
          className="user-menu-chevron"
          width="10"
          height="10"
          viewBox="0 0 10 10"
          fill="none"
          stroke="currentColor"
          strokeWidth="1.6"
          strokeLinecap="round"
          strokeLinejoin="round"
          style={{ color: 'var(--tx-md)', flexShrink: 0 }}
        >
          <path d="M2 3.5L5 6.5L8 3.5" />
        </svg>
      </button>

      <div
        className="user-menu-dropdown"
        role="menu"
        aria-hidden={!open}
        aria-label="Cuenta"
        style={{
          position: 'absolute',
          right: 0,
          top: 'calc(100% + 8px)',
          minWidth: 200,
          maxWidth: 'calc(100vw - 24px)',
          background: 'var(--bg-card)',
          border: '1px solid var(--bd)',
          borderRadius: 'var(--radius-card, 7px)',
          boxShadow: 'var(--shadow-md)',
          padding: 8,
          zIndex: 50,
          fontFamily: 'Inter, system-ui',
        }}
      >
        <div style={{ padding: '6px 8px 10px', borderBottom: '1px solid var(--bd)' }}>
          <div style={{ fontSize: 13, fontWeight: 600, color: 'var(--tx-hi)' }}>
            {user?.nombre ?? 'Usuario'}
          </div>
          <div style={{ fontSize: 11, color: 'var(--tx-lo)', marginTop: 2, overflowWrap: 'anywhere' }}>
            {user?.email ?? ''}
          </div>
        </div>
        <button
          type="button"
          role="menuitem"
          className="nr-account-logout"
          onClick={() => {
            setOpen(false)
            logout()
          }}
          style={{
            width: '100%',
            marginTop: 6,
            minHeight: 40,
            border: 'none',
            borderRadius: 6,
            background: 'transparent',
            color: 'var(--er-tx)',
            fontSize: 13,
            fontWeight: 500,
            cursor: 'pointer',
            textAlign: 'left',
            padding: '0 8px',
            fontFamily: 'Inter, system-ui',
          }}
        >
          Cerrar sesión
        </button>
      </div>
    </div>
  )
}
