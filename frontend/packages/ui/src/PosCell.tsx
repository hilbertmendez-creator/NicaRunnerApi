import { Medal, type MedalRank } from './Medal'

interface PosCellProps {
  rank: number
  className?: string
}

export function PosCell({ rank, className = '' }: PosCellProps) {
  const isPodium = rank >= 1 && rank <= 3
  return (
    <span className={`inline-flex items-center gap-1.5 ${className}`}>
      <span className="font-mono text-sm font-medium tabular-nums text-zinc-900">{rank}</span>
      {isPodium && <Medal rank={rank as MedalRank} />}
    </span>
  )
}