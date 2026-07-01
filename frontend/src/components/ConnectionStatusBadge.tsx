export type ConnectionState = 'online' | 'syncing' | 'offline'

const CONFIG: Record<ConnectionState, { label: string; dot: string; pulse?: boolean }> = {
  online: { label: 'En línea', dot: 'var(--badge-ok-text)' },
  syncing: { label: 'Sincronizando…', dot: 'var(--accent)', pulse: true },
  offline: { label: 'Sin conexión · datos en caché', dot: 'var(--conflict-bd)' },
}

interface ConnectionStatusBadgeProps {
  state: ConnectionState
  onClick?: () => void
}

export function ConnectionStatusBadge({ state, onClick }: ConnectionStatusBadgeProps) {
  const cfg = CONFIG[state]
  return (
    <span
      onClick={onClick}
      title={onClick ? 'Simular cambio de estado de conexión' : undefined}
      className={`inline-flex items-center gap-1.5 text-xs font-medium ${onClick ? 'cursor-pointer' : ''}`}
      style={{ color: 'var(--text-lo)' }}
    >
      <span
        className={`h-1.5 w-1.5 rounded-full ${cfg.pulse ? 'motion-safe:animate-pulse' : ''}`}
        style={{ background: cfg.dot }}
      />
      {cfg.label}
    </span>
  )
}
