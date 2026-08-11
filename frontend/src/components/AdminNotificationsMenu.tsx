import { useEffect, useRef, useState } from 'react'
import { getAdminNotifications, markAdminNotificationRead, markAllAdminNotificationsRead } from '../api/endpoints'
import { usePolling } from '../hooks/usePolling'
import type { AdminNotificationDto, AdminNotificationType } from '../api/types'

const POLL_INTERVAL_MS = 20000

// Record completo a propósito: si el backend agrega un AdminNotificationType, TypeScript
// rompe acá hasta que la campana sepa cómo nombrarlo. Nunca una etiqueta vacía en la UI.
const TYPE_LABEL: Record<AdminNotificationType, string> = {
  DorsalDuplicado: 'Dorsal duplicado',
  UsuarioCreado: 'Usuario creado',
  ImportacionConErrores: 'Importación con errores',
  CarreraCerradaPorJuez: 'Carrera cerrada por un juez',
  CarrerasSinActividad: 'Carreras sin actividad',
}

/** Campana del topbar: feed de eventos administrativos, reusa el patrón de UserAccountMenu (.user-menu/.open). */
export function AdminNotificationsMenu() {
  const [open, setOpen] = useState(false)
  const rootRef = useRef<HTMLDivElement>(null)
  const { data, refetch } = usePolling(() => getAdminNotifications(20), POLL_INTERVAL_MS, [])

  const items = data?.items ?? []
  const unreadCount = data?.unreadCount ?? 0

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

  async function handleItemClick(item: AdminNotificationDto) {
    if (item.leida) return
    await markAdminNotificationRead(item.id)
    refetch()
  }

  async function handleMarkAllRead() {
    await markAllAdminNotificationsRead()
    refetch()
  }

  return (
    <div ref={rootRef} className={`user-menu ${open ? 'open' : ''}`} style={{ position: 'relative' }}>
      <button
        type="button"
        className="nr-icon-btn"
        aria-label={unreadCount > 0 ? `Notificaciones (${unreadCount} sin leer)` : 'Notificaciones'}
        aria-expanded={open}
        aria-haspopup="menu"
        onClick={() => setOpen((o) => !o)}
        style={{
          width: 32,
          height: 32,
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
        {unreadCount > 0 && (
          <span
            aria-hidden="true"
            style={{
              position: 'absolute',
              top: 2,
              right: 2,
              minWidth: 14,
              height: 14,
              padding: '0 3px',
              borderRadius: 7,
              background: 'var(--er-tx, #EF4444)',
              color: '#fff',
              fontSize: 9,
              fontWeight: 700,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              lineHeight: 1,
              fontFamily: 'Inter, system-ui',
            }}
          >
            {unreadCount > 9 ? '9+' : unreadCount}
          </span>
        )}
      </button>

      <div
        className="user-menu-dropdown"
        role="menu"
        aria-hidden={!open}
        aria-label="Notificaciones"
        style={{
          position: 'absolute',
          right: 0,
          top: 'calc(100% + 8px)',
          width: 'min(320px, calc(100vw - 24px))',
          maxHeight: 400,
          overflowY: 'auto',
          background: 'var(--bg-card)',
          border: '1px solid var(--bd)',
          borderRadius: 'var(--radius-card, 7px)',
          boxShadow: 'var(--shadow-md)',
          zIndex: 50,
          fontFamily: 'Inter, system-ui',
        }}
      >
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            padding: '10px 12px',
            borderBottom: '1px solid var(--bd)',
          }}
        >
          <span style={{ fontSize: 13, fontWeight: 600, color: 'var(--tx-hi)' }}>Notificaciones</span>
          {unreadCount > 0 && (
            <button
              type="button"
              role="menuitem"
              onClick={handleMarkAllRead}
              style={{ border: 'none', background: 'transparent', color: 'var(--ac, #1d4ed8)', fontSize: 12, cursor: 'pointer', padding: 0 }}
            >
              Marcar todas como leídas
            </button>
          )}
        </div>

        {items.length === 0 && (
          <p style={{ padding: '16px 12px', fontSize: 13, color: 'var(--tx-lo)' }}>Sin notificaciones.</p>
        )}

        {items.map((item) => (
          <button
            key={item.id}
            type="button"
            role="menuitem"
            onClick={() => handleItemClick(item)}
            style={{
              display: 'block',
              width: '100%',
              textAlign: 'left',
              padding: '10px 12px',
              border: 'none',
              borderBottom: '1px solid var(--bd)',
              background: item.leida ? 'transparent' : 'var(--bg-hover)',
              cursor: 'pointer',
            }}
          >
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 6,
                fontSize: 11,
                fontWeight: 600,
                textTransform: 'uppercase',
                letterSpacing: '.4px',
                color: 'var(--tx-lo)',
              }}
            >
              {TYPE_LABEL[item.type]}
              {!item.leida && (
                <span style={{ color: 'var(--ac, #1d4ed8)', textTransform: 'none', letterSpacing: 0 }}>
                  · Sin leer
                </span>
              )}
            </div>
            <div style={{ fontSize: 13, color: 'var(--tx-hi)', marginTop: 2 }}>{item.mensaje}</div>
            <div style={{ fontSize: 11, color: 'var(--tx-lo)', marginTop: 2 }}>
              {new Date(item.createdAt).toLocaleString('es-NI')}
            </div>
          </button>
        ))}
      </div>
    </div>
  )
}
