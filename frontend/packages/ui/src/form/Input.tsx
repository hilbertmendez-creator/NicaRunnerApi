import { forwardRef, type InputHTMLAttributes } from 'react'

export const Input = forwardRef<HTMLInputElement, InputHTMLAttributes<HTMLInputElement>>(
  function Input({ className = '', ...rest }, ref) {
    return (
      <input
        ref={ref}
        className={`h-8 border border-zinc-200 bg-white px-3 text-sm text-zinc-900 focus:border-blue-700 focus:outline-none focus:ring-1 focus:ring-blue-700 ${className}`}
        {...rest}
      />
    )
  },
)
