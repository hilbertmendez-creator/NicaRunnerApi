export type ConnectionState = 'online' | 'syncing' | 'offline'

const CONFIG: Record<ConnectionState, { label: string; dot: string; pulse?: boolean }> = {
  online: { label: 'En línea', dot: 'bg-official-600' },
  syncing: { label: 'Sincronizando…', dot: 'bg-blue-700', pulse: true },
  offline: { label: 'Sin conexión · datos en caché', dot: 'bg-critical-600' },
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
      className={`inline-flex items-center gap-1.5 text-xs font-medium text-zinc-600 ${onClick ? 'cursor-pointer' : ''}`}
    >
      <span className={`h-1.5 w-1.5 rounded-full ${cfg.dot} ${cfg.pulse ? 'motion-safe:animate-pulse' : ''}`} />
      {cfg.label}
    </span>
  )
}
