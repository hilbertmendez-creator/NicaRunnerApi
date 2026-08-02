import { useEffect, useState } from 'react'
import { useAuth } from '../auth/auth-context'

/** Avatar con iniciales + menú de cuenta (logout explícito). Sin foto en API. */
export function UserAccountMenu() {
  const { user, logout } = useAuth()
  const [open, setOpen] = useState(false)

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

  return (
    <div style={{ position: 'relative', marginTop: 'auto' }}>
      <button
        type="button"
        className="nr-account-btn"
        title={user?.nombre ?? 'Mi cuenta'}
        aria-label="Menú de cuenta"
        aria-expanded={open}
        aria-haspopup="menu"
        onClick={() => setOpen((o) => !o)}
        style={{
          width: 36,
          height: 36,
          borderRadius: '50%',
          background: 'var(--sb-hover)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          fontSize: 12,
          fontWeight: 600,
          color: 'var(--sb-fg)',
          cursor: 'pointer',
          border: open ? '1.5px solid var(--sb-sep)' : '1.5px solid transparent',
          fontFamily: 'Inter, system-ui',
        }}
      >
        {initials}
      </button>
      {open && (
        <>
          <div
            aria-hidden="true"
            onClick={() => setOpen(false)}
            style={{ position: 'fixed', inset: 0, zIndex: 40 }}
          />
          <div
            role="menu"
            aria-label="Cuenta"
            style={{
              position: 'absolute',
              left: 'calc(100% + 8px)',
              bottom: 0,
              minWidth: 200,
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
        </>
      )}
    </div>
  )
}
