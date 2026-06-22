import type { RaceStatus } from '../api/types'

const STYLES: Record<RaceStatus, string> = {
  Planeada: 'border-zinc-200 bg-zinc-50 text-zinc-600',
  EnCurso: 'border-official-200 bg-official-50 text-official-600',
  Terminada: 'border-blue-200 bg-blue-50 text-blue-700',
}

const LABELS: Record<RaceStatus, string> = {
  Planeada: 'Planeada',
  EnCurso: 'En curso',
  Terminada: 'Terminada',
}

export function StatusBadge({ status }: { status: RaceStatus }) {
  return (
    <span className={`inline-flex items-center gap-1.5 border px-2 py-0.5 text-xs font-medium ${STYLES[status]}`}>
      {status === 'EnCurso' && (
        <span className="h-1.5 w-1.5 rounded-full bg-official-600 motion-safe:animate-pulse" />
      )}
      {LABELS[status]}
    </span>
  )
}
