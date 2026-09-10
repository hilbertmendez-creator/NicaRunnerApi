import type { CSSProperties } from 'react'
import type { RaceStatus } from '../api/types'

type Tone = 'ok' | 'neutral' | 'muted'

const TONE_STYLES: Record<Tone, CSSProperties> = {
  ok: {
    background: 'var(--badge-ok-bg)',
    color: 'var(--badge-ok-text)',
    border: '1px solid var(--badge-ok-bd)',
  },
  neutral: {
    background: 'var(--badge-cl-bg)',
    color: 'var(--badge-cl-text)',
    border: '1px solid var(--badge-cl-bd)',
  },
  muted: {
    background: 'var(--badge-pr-bg)',
    color: 'var(--badge-pr-text)',
    border: '1px solid var(--badge-pr-bd)',
  },
}

function Badge({ tone, label, live }: { tone: Tone; label: string; live?: boolean }) {
  return (
    <span
      className="inline-flex items-center gap-1.5 px-2 py-0.5 text-xs font-medium"
      style={{ ...TONE_STYLES[tone], borderRadius: 'var(--radius-badge)' }}
    >
      {live && <span className="dot-live" />}
      {label}
    </span>
  )
}

const RACE_STATUS_TONE: Record<RaceStatus, Tone> = {
  Planeada: 'neutral',
  EnCurso: 'ok',
  Terminada: 'muted',
}

const RACE_STATUS_LABELS: Record<RaceStatus, string> = {
  Planeada: 'Planeada',
  EnCurso: 'En curso',
  Terminada: 'Terminada',
}

export function StatusBadge({ status }: { status: RaceStatus }) {
  return (
    <Badge
      tone={RACE_STATUS_TONE[status]}
      label={RACE_STATUS_LABELS[status]}
      live={status === 'EnCurso'}
    />
  )
}

export function UserStatusBadge({ isActive }: { isActive: boolean }) {
  return <Badge tone={isActive ? 'ok' : 'neutral'} label={isActive ? 'Activo' : 'Inactivo'} />
}
