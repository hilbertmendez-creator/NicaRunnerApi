import type { CSSProperties } from 'react'
import type { RaceStatus } from '../api/types'

const STYLES: Record<RaceStatus, CSSProperties> = {
  Planeada: {
    background: 'var(--badge-cl-bg)',
    color: 'var(--badge-cl-text)',
    border: '1px solid var(--bd)',
  },
  EnCurso: {
    background: 'var(--badge-ok-bg)',
    color: 'var(--badge-ok-text)',
    border: '1px solid var(--badge-ok-bd)',
  },
  Terminada: {
    background: 'var(--badge-pr-bg)',
    color: 'var(--badge-pr-text)',
    border: '1px solid var(--badge-pr-bd)',
  },
}

const LABELS: Record<RaceStatus, string> = {
  Planeada: 'Planeada',
  EnCurso: 'En curso',
  Terminada: 'Terminada',
}

export function StatusBadge({ status }: { status: RaceStatus }) {
  return (
    <span
      className="inline-flex items-center gap-1.5 px-2 py-0.5 text-xs font-medium"
      style={{ ...STYLES[status], borderRadius: 'var(--radius-badge)' }}
    >
      {status === 'EnCurso' && (
        <span className="dot-live" />
      )}
      {LABELS[status]}
    </span>
  )
}
