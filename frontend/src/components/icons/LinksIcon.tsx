import type { IconProps } from './types'

export function LinksIcon({ size = 16, className }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 20 20" fill="none" className={className} aria-hidden="true">
      <path
        d="M8 10h4M8 10a3.5 3.5 0 010-5h4a3.5 3.5 0 010 5M8 10a3.5 3.5 0 000 5h4a3.5 3.5 0 000-5"
        stroke="currentColor"
        strokeWidth="1.5"
      />
    </svg>
  )
}
