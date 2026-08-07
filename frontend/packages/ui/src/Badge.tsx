import type { ReactNode } from 'react'

export type BadgeVariant = 'success' | 'warning' | 'error' | 'info' | 'neutral'

interface BadgeProps {
  variant?: BadgeVariant
  live?: boolean
  className?: string
  children: ReactNode
}

const VARIANT_CLASSES: Record<BadgeVariant, string> = {
  success: 'bg-official-50 text-official-600',
  warning: 'bg-dispute-50 text-dispute-600',
  error: 'bg-critical-50 text-critical-600',
  info: 'bg-blue-50 text-blue-700',
  neutral: 'bg-zinc-100 text-zinc-600',
}

export function Badge({ variant = 'neutral', live = false, className = '', children }: BadgeProps) {
  return (
    <span
      className={`inline-flex items-center gap-1.5 px-2 py-0.5 text-xs font-medium ${VARIANT_CLASSES[variant]} ${className}`}
      style={{ borderRadius: 'var(--radius-badge)' }}
    >
      {live && <span className="dot-live" aria-hidden="true" />}
      {children}
    </span>
  )
}