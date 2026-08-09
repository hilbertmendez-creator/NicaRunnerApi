export interface PositionBadgeProps {
  position?: number | null
}

const MEDALS = {
  1: { core: 'var(--medal-gold-core)', face: 'var(--medal-gold-face)', edge: 'var(--medal-gold-edge)' },
  2: { core: 'var(--medal-silver-core)', face: 'var(--medal-silver-face)', edge: 'var(--medal-silver-edge)' },
  3: { core: 'var(--medal-bronze-core)', face: 'var(--medal-bronze-face)', edge: 'var(--medal-bronze-edge)' },
} as const

/** Renderiza el podio: posiciones 1-3 llevan medalla, el resto solo el número. */
export function PositionBadge({ position }: PositionBadgeProps) {
  // Este chequeo corre primero: cubre null/undefined/0/negativos y cualquier
  // llamador que mapee DNF a un valor "sin posición".
  if (position == null || !Number.isFinite(position) || position < 1) {
    return (
      <span aria-label="Sin posición" style={{ color: 'var(--tx-lo)' }}>
        —
      </span>
    )
  }

  if (position <= 3) {
    const medal = MEDALS[position as 1 | 2 | 3]
    return (
      <span className="pos-cell" style={{ display: 'inline-flex', flexDirection: 'row-reverse', alignItems: 'center', gap: 4 }}>
        <svg viewBox="0 0 20 20" width="16" height="16" aria-hidden="true" focusable="false">
          <path d="M7 3h6l-1 3H8L7 3z" fill={medal.core} />
          <circle cx="10" cy="12" r="5.5" fill={medal.face} stroke={medal.edge} strokeWidth="0.8" />
          <circle cx="10" cy="12" r="3" fill={medal.core} />
        </svg>
        <span className="pos-num">{position}</span>
      </span>
    )
  }

  return <span className="pos-num">{position}</span>
}
