import { forwardRef, type InputHTMLAttributes } from 'react'

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  invalid?: boolean
}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  function Input({ className = '', invalid = false, ...rest }, ref) {
    return (
      <input
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
