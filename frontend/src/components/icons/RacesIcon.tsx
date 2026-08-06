import type { IconProps } from './types'

export function RacesIcon({ size = 16, className }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 20 20" fill="none" className={className} aria-hidden="true">
      <path d="M7 3h6l2 5-5 2-5-2 2-5z" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round" />
      <path d="M4 17v-7m12 7v-7M10 13v4" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
    </svg>
  )
}
