import type { IconProps } from './types'

export function UsersIcon({ size = 16, className }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 20 20" fill="none" className={className} aria-hidden="true">
      <circle cx="7" cy="6" r="3" stroke="currentColor" strokeWidth="1.5" />
      <circle cx="13" cy="6" r="3" stroke="currentColor" strokeWidth="1.5" />
      <path d="M1 18c0-3.3 2.7-6 6-6s6 2.7 6 6M19 18c0-3.3-2.7-6-6-6" stroke="currentColor" strokeWidth="1.5" />
    </svg>
  )
}
