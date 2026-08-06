import type { IconProps } from './types'

export function ResultsIcon({ size = 16, className }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 20 20" fill="none" className={className} aria-hidden="true">
      <path d="M3 3h14v14H3V3z" stroke="currentColor" strokeWidth="1.5" />
      <path d="M7 14V8m3 6v-4m3 4v-6" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
    </svg>
  )
}
