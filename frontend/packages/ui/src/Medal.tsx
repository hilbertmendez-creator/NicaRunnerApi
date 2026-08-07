export type MedalRank = 1 | 2 | 3

interface MedalProps {
  rank: MedalRank
  size?: number
  className?: string
}

/** Colores de la medalla según la posición (referencia Race-Day Control Room). */
const MEDAL_COLORS: Record<MedalRank, { ribbon: string; disc: string; stroke: string; core: string }> = {
  1: { ribbon: '#F59E0B', disc: '#FBBF24', stroke: '#D97706', core: '#F59E0B' },
  2: { ribbon: '#94A3B8', disc: '#CBD5E1', stroke: '#94A3B8', core: '#94A3B8' },
  3: { ribbon: '#B45309', disc: '#D97706', stroke: '#92400E', core: '#B45309' },
}

export function Medal({ rank, size = 20, className = '' }: MedalProps) {
  const colors = MEDAL_COLORS[rank]
  return (
    <svg
      viewBox="0 0 20 20"
      fill="none"
      width={size}
      height={size}
      className={className}
      aria-hidden="true"
    >
      <path d="M7 3h6l-1 3H8L7 3z" fill={colors.ribbon} />
      <circle cx="10" cy="12" r="5.5" fill={colors.disc} stroke={colors.stroke} strokeWidth="0.8" />
      <circle cx="10" cy="12" r="3" fill={colors.core} />
    </svg>
  )
}