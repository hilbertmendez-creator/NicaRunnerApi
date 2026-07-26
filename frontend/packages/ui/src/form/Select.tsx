import { forwardRef, type SelectHTMLAttributes } from 'react'

interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  invalid?: boolean
}

export const Select = forwardRef<HTMLSelectElement, SelectProps>(
  function Select({ className = '', invalid = false, ...rest }, ref) {
    return (
      <select
        ref={ref}
        aria-invalid={invalid || undefined}
        className={`h-8 border bg-white px-3 text-sm text-zinc-900 focus:outline-none focus:ring-1 ${
          invalid
            ? 'border-critical-600 focus:border-critical-600 focus:ring-critical-600'
            : 'border-zinc-200 focus:border-blue-700 focus:ring-blue-700'
        } ${className}`}
        {...rest}
      />
    )
  },
)
