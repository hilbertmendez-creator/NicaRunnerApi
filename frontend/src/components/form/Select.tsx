import { forwardRef, type SelectHTMLAttributes } from 'react'

export const Select = forwardRef<HTMLSelectElement, SelectHTMLAttributes<HTMLSelectElement>>(
  function Select({ className = '', ...rest }, ref) {
    return (
      <select
        ref={ref}
        className={`rounded border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none ${className}`}
        {...rest}
      />
    )
  },
)
