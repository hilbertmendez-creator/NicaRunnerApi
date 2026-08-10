import type { LabelHTMLAttributes, ReactNode } from 'react'

interface LabelProps extends LabelHTMLAttributes<HTMLLabelElement> {
  required?: boolean
  children?: ReactNode
}

export function Label({ className = '', style, required = false, children, ...rest }: LabelProps) {
  return (
    <label
      className={`mb-1 block text-sm font-medium ${className}`}
      style={{ color: 'var(--text-lo, #52525b)', ...style }}
      {...rest}
    >
      {children}
      {required && (
        <span aria-hidden="true" style={{ color: 'var(--er-tx, #dc2626)' }} title="Campo requerido">
          {' '}
          *
        </span>
      )}
    </label>
  )
}
