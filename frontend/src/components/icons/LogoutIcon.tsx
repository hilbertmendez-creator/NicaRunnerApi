import type { IconProps } from './types'

export function LogoutIcon({ size = 15, className }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 20 20" fill="none" className={className} aria-hidden="true">
      <path
        d="M7 17H4a2 2 0 01-2-2V5a2 2 0 012-2h3M11 14l4-4-4-4M15 10H7"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  )
}
