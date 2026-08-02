import type { CSSProperties } from 'react'

type MetricCardVariant = 'gray' | 'orange' | 'teal' | 'amber' | 'red'

interface MetricCardProps {
  label: string
  value: string | number
  variant?: MetricCardVariant
  size?: 'sm' | 'md'
  className?: string
}

const VARIANT_STYLES: Record<MetricCardVariant, { wrap: CSSProperties; label: CSSProperties; value: CSSProperties }> = {
  gray: {
    wrap: {
      background: 'var(--bg-card, #fff)',
      border: '1px solid var(--bd, #e4e4e7)',
    },
    label: { color: 'var(--text-lo, #71717a)' },
    value: { color: 'var(--text-hi, #18181b)' },
  },
  orange: {
    wrap: {
      background: 'var(--wn-bg, #fffbeb)',
      border: '1px solid var(--wn-bd, #fde68a)',
    },
    label: { color: 'var(--wn-tx, #92400e)' },
    value: { color: 'var(--wn-tx, #92400e)' },
  },
  teal: {
    wrap: {
      background: 'var(--ok-bg, #f0fdf4)',
      border: '1px solid var(--ok-bd, #bbf7d0)',
    },
    label: { color: 'var(--ok-tx, #15803d)' },
    value: { color: 'var(--ok-tx, #15803d)' },
  },
  amber: {
    wrap: {
      background: 'var(--wn-bg, #fffbeb)',
      border: '1px solid var(--wn-bd, #fde68a)',
    },
    label: { color: 'var(--wn-tx, #92400e)' },
    value: { color: 'var(--wn-tx, #92400e)' },
  },
  red: {
    wrap: {
      background: 'var(--er-bg, #fef2f2)',
      border: '1px solid var(--er-bd, #fecaca)',
    },
    label: { color: 'var(--er-tx, #b91c1c)' },
    value: { color: 'var(--er-tx, #b91c1c)' },
  },
}

const SIZE_CLASSES = {
  sm: { p: 'p-2.5', label: 'text-xs', value: 'text-lg' },
  md: { p: 'p-3', label: 'text-xs', value: 'text-xl' },
}

export function MetricCard({ label, value, variant = 'gray', size = 'md', className = '' }: MetricCardProps) {
  const styles = VARIANT_STYLES[variant]
  const sizeStyles = SIZE_CLASSES[size]

  return (
    <div
      className={`${sizeStyles.p} ${className}`}
      style={{
        ...styles.wrap,
        borderRadius: 'var(--radius-card, 7px)',
      }}
    >
      <p
        className={`${sizeStyles.label} mb-1 font-medium uppercase tracking-wide`}
        style={styles.label}
      >
        {label}
      </p>
      <p className={`${sizeStyles.value} font-mono font-semibold tabular-nums`} style={styles.value}>
        {value}
      </p>
    </div>
  )
}
