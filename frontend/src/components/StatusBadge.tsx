import type { RaceStatus } from '../api/types'

const STYLES: Record<RaceStatus, string> = {
  Planeada: 'bg-gray-100 text-gray-700',
  EnCurso: 'bg-green-100 text-green-800',
  Terminada: 'bg-blue-100 text-blue-800',
}

const LABELS: Record<RaceStatus, string> = {
  Planeada: 'Planeada',
  EnCurso: 'En curso',
  Terminada: 'Terminada',
}

export function StatusBadge({ status }: { status: RaceStatus }) {
  return (
    <span className={`rounded-md px-2.5 py-1 text-xs font-medium ${STYLES[status]}`}>
      {LABELS[status]}
    </span>
  )
}
