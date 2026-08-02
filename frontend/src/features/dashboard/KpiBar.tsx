interface KpiItem {
  label: string
  value: string
  /** Color del valor principal (p. ej. warning en pendientes). */
  valueColor?: string
  trend?: string
  /** Color del subtítulo de tendencia; por defecto muted, nunca verde forzado. */
  trendColor?: string
}

interface KpiBarProps {
  items: KpiItem[]
}

export function KpiBar({ items }: KpiBarProps) {
  return (
    <div
      style={{
        background: 'var(--bg-card)',
        border: '1px solid var(--bd)',
        borderRadius: 'var(--r-card)',
        padding: '11px 16px',
        display: 'flex',
        alignItems: 'center',
        gap: 20,
        marginBottom: 14,
        boxShadow: 'var(--shadow-sm)',
        overflowX: 'auto',
      }}
    >
      {items.map((item, i) => (
        <div key={item.label} style={{ display: 'contents' }}>
          {i > 0 && (
            <div
              style={{ width: 1, height: 28, background: 'var(--bd-inner)', flexShrink: 0 }}
            />
          )}
          <div style={{ display: 'flex', flexDirection: 'column', gap: 1, flexShrink: 0 }}>
            <span
              style={{
                fontSize: 10,
                fontWeight: 600,
                textTransform: 'uppercase',
                letterSpacing: '.45px',
                color: 'var(--tx-lo)',
              }}
            >
              {item.label}
            </span>
            <span
              style={{
                fontSize: 15,
                fontWeight: 700,
                color: item.valueColor ?? 'var(--tx-hi)',
                fontFeatureSettings: '"tnum"',
                fontFamily: '"IBM Plex Mono", ui-monospace, monospace',
              }}
            >
              {item.value}
            </span>
            {item.trend && (
              <span style={{ fontSize: 10, color: item.trendColor ?? 'var(--tx-lo)' }}>
                {item.trend}
              </span>
            )}
          </div>
        </div>
      ))}
    </div>
  )
}
