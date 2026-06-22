type MetricCardVariant = 'gray' | 'orange' | 'teal' | 'amber' | 'red'

interface MetricCardProps {
  label: string
  value: string | number
  variant?: MetricCardVariant
  size?: 'sm' | 'md'
  className?: string
}

const VARIANT_CLASSES: Record<MetricCardVariant, { bg: string; label: string; value: string }> = {
  gray: { bg: 'bg-white border border-zinc-200', label: 'text-zinc-500', value: 'text-zinc-900' },
  orange: { bg: 'bg-dispute-50 border border-dispute-200', label: 'text-dispute-600', value: 'text-dispute-600' },
  teal: { bg: 'bg-official-50 border border-official-200', label: 'text-official-600', value: 'text-official-600' },
  amber: { bg: 'bg-dispute-50 border border-dispute-200', label: 'text-dispute-600', value: 'text-dispute-600' },
  red: { bg: 'bg-critical-50 border border-critical-200', label: 'text-critical-600', value: 'text-critical-600' },
}

const SIZE_CLASSES = {
  sm: { p: 'p-2.5', label: 'text-xs', value: 'text-lg' },
  md: { p: 'p-3', label: 'text-xs', value: 'text-xl' },
}

export function MetricCard({ label, value, variant = 'gray', size = 'md', className = '' }: MetricCardProps) {
  const styles = VARIANT_CLASSES[variant]
  const sizeStyles = SIZE_CLASSES[size]

  return (
    <div className={`${styles.bg} ${sizeStyles.p} ${className}`}>
      <p className={`${styles.label} ${sizeStyles.label} mb-1 font-medium uppercase tracking-wide`}>{label}</p>
      <p className={`${styles.value} ${sizeStyles.value} font-mono font-semibold tabular-nums`}>{value}</p>
    </div>
  )
}
