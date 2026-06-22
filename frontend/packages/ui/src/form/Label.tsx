import type { LabelHTMLAttributes } from 'react'

export function Label({ className = '', ...rest }: LabelHTMLAttributes<HTMLLabelElement>) {
  return (
    <label
      className={`mb-1 block text-sm font-medium text-zinc-700 ${className}`}
      {...rest}
    />
  )
}
