import { forwardRef, type SelectHTMLAttributes } from 'react'

export const Select = forwardRef<HTMLSelectElement, SelectHTMLAttributes<HTMLSelectElement>>(
  function Select({ className = '', ...rest }, ref) {
    return (
      <select
        ref={ref}
        className={`h-8 border border-zinc-200 bg-white px-3 text-sm text-zinc-900 focus:border-blue-700 focus:outline-none focus:ring-1 focus:ring-blue-700 ${className}`}
        {...rest}
      />
    )
  },
)
