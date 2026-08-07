interface ProgressProps {
  /** Porcentaje único (relleno official verde). Alternativa a official/dispute. */
  value?: number
  /** Porcentaje official (verde). */
  official?: number
  /** Porcentaje dispute (ámbar). */
  dispute?: number
  className?: string
}

interface Segment {
  key: string
  pct: number
  color: string
}

/** Barra de progreso de 6px (referencia Race-Day Control Room).
 *  `value` es un único relleno official verde; `official`+`dispute` pintan un
 *  segundo segmento usando --color-dispute-600. */
export function Progress({ value, official, dispute, className = '' }: ProgressProps) {
  const officialPct = official ?? value ?? 0
  const disputePct = dispute ?? 0
  const caps = (pct: number) => Math.min(100, Math.max(0, pct))

  const segments: Segment[] = [{ key: 'official', pct: caps(officialPct), color: 'var(--color-official-600)' }]
  if (disputePct > 0) {
    segments.push({ key: 'dispute', pct: caps(disputePct), color: 'var(--color-dispute-600)' })
  }

  return (
    <div
      className={`flex h-1.5 min-w-20 flex-1 overflow-hidden rounded ${className}`}
      style={{ background: 'var(--bg-hover)' }}
    >
      {segments.map((seg) => (
        <div
          key={seg.key}
          style={{ width: `${seg.pct}%`, background: seg.color }}
        />
      ))}
    </div>
  )
}