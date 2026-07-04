import { forwardRef, type SelectHTMLAttributes } from 'react'

export const Select = forwardRef<HTMLSelectElement, SelectHTMLAttributes<HTMLSelectElement>>(
  function Select({ className = '', ...rest }, ref) {
    return (
      <select
        ref={ref}
        className={`nr-input h-8 px-3 text-sm ${className}`}
        {...rest}
      />
    )
  },
)
