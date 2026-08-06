import type { IconProps } from './types'

export function CategoriesIcon({ size = 16, className }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 20 20" fill="none" className={className} aria-hidden="true">
      <rect x="2" y="2" width="7" height="6" rx="1" stroke="currentColor" strokeWidth="1.5" />
      <rect x="11" y="2" width="7" height="10" rx="1" stroke="currentColor" strokeWidth="1.5" />
      <rect x="2" y="10" width="7" height="8" rx="1" stroke="currentColor" strokeWidth="1.5" />
    </svg>
  )
}
