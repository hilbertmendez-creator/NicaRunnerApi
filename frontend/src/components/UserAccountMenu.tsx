import { useState } from 'react'
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

  return (
    <div style={{ position: 'relative', marginTop: 'auto' }}>
      <button
        type="button"
        title={user?.nombre ?? 'Mi cuenta'}
        aria-label="Menú de cuenta"
        aria-expanded={open}
        aria-haspopup="menu"
        onClick={() => setOpen((o) => !o)}
        style={{
          width: 44,
          height: 44,
          borderRadius: '50%',
          background: 'rgba(255,255,255,.1)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          fontSize: 12,
          fontWeight: 600,
          color: 'rgba(255,255,255,.7)',
          cursor: 'pointer',
          border: open ? '1.5px solid rgba(255,255,255,.35)' : '1.5px solid transparent',
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
              borderRadius: 8,
              boxShadow: 'var(--shadow-sm)',
              padding: 8,
              zIndex: 50,
              fontFamily: 'Inter, system-ui',
            }}
          >
            <div style={{ padding: '6px 8px 10px', borderBottom: '1px solid var(--bd)' }}>
              <div style={{ fontSize: 13, fontWeight: 600, color: 'var(--tx-hi)' }}>
                {user?.nombre ?? 'Usuario'}
              </div>
              <div style={{ fontSize: 11, color: 'var(--tx-lo)', marginTop: 2 }}>
                {user?.email ?? ''}
              </div>
            </div>
            <button
              type="button"
              role="menuitem"
              onClick={() => {
                setOpen(false)
                logout()
              }}
              style={{
                width: '100%',
                marginTop: 6,
                minHeight: 44,
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
