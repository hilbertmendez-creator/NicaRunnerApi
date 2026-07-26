import type { LabelHTMLAttributes } from 'react'

export function Label({ className = '', style, ...rest }: LabelHTMLAttributes<HTMLLabelElement>) {
  return (
    <label
      className={`mb-1 block text-sm font-medium ${className}`}
      style={{ color: 'var(--text-lo, #52525b)', ...style }}
      {...rest}
    />
  )
}
